﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ClassicAssist.Data;
using ClassicAssist.Data.Abilities;
using ClassicAssist.Data.Commands;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UI.Views;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network;
using ClassicAssist.UO.Network.PacketFilter;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using CUO_API;
using Octokit;

[assembly: InternalsVisibleTo( "ClassicAssist.Tests" )]

// ReSharper disable once CheckNamespace
namespace Assistant
{
    public static class Engine
    {
        public delegate void dConnected();

        public delegate void dDisconnected();

        public delegate void dPlayerInitialized( PlayerMobile player );

        public delegate void dSendRecvPacket( byte[] data, int length );

        private const int MAX_DISTANCE = 32;

        private static OnConnected _onConnected;
        private static OnDisconnected _onDisconnected;
        private static OnPacketSendRecv _onReceive;
        private static OnPacketSendRecv _onSend;
        private static OnGetUOFilePath _getUOFilePath;
        private static OnPacketSendRecv _sendToClient;
        private static OnPacketSendRecv _sendToServer;
        private static OnGetPacketLength _getPacketLength;
        private static ThreadQueue<Packet> _incomingQueue;
        private static OnUpdatePlayerPosition _onPlayerPositionChanged;
        private static ThreadQueue<Packet> _outgoingQueue;
        private static MainWindow _window;
        private static Thread _mainThread;
        private static OnClientClose _onClientClosing;
        private static readonly PacketFilter _incomingPacketFilter = new PacketFilter();
        private static readonly PacketFilter _outgoingPacketFilter = new PacketFilter();
        private static OnHotkey _onHotkeyPressed;
        private static RequestMove _requestMove;

        private static readonly int[] _sequenceList = new int[256];
        private static OnMouse _onMouse;

        public static string ClientPath { get; set; }
        public static Version ClientVersion { get; set; }
        public static bool Connected { get; set; }
        public static Dispatcher Dispatcher { get; set; }
        public static FeatureFlags Features { get; set; }
        public static GumpCollection Gumps { get; set; } = new GumpCollection();
        public static ItemCollection Items { get; set; } = new ItemCollection( 0 );
        public static CircularBuffer<JournalEntry> Journal { get; set; } = new CircularBuffer<JournalEntry>( 1024 );
        public static MobileCollection Mobiles { get; set; } = new MobileCollection( Items );
        public static PacketWaitEntries PacketWaitEntries { get; set; }
        public static PlayerMobile Player { get; set; }
        public static RehueList RehueList { get; set; } = new RehueList();
        public static string StartupPath { get; set; }
        public static bool TargetExists { get; set; }
        public static TargetFlags TargetFlags { get; set; }
        public static int TargetSerial { get; set; }
        public static TargetType TargetType { get; set; }
        public static ThreadQueue<int> UseObjectQueue { get; set; } = new ThreadQueue<int>( ProcessUseObjectQueue );
        public static bool WaitingForTarget { get; set; }
        internal static ConcurrentDictionary<int, int> GumpList { get; set; } = new ConcurrentDictionary<int, int>();

        internal static event dSendRecvPacket InternalPacketSentEvent;
        internal static event dSendRecvPacket InternalPacketReceivedEvent;

        public static event dSendRecvPacket PacketReceivedEvent;
        public static event dSendRecvPacket PacketSentEvent;
        public static event dSendRecvPacket SentPacketFilteredEvent;
        public static event dSendRecvPacket ReceivedPacketFilteredEvent;
        public static event dConnected ConnectedEvent;
        public static event dDisconnected DisconnectedEvent;
        public static event dPlayerInitialized PlayerInitializedEvent;

        public static unsafe void Install( PluginHeader* plugin )
        {
            Initialize();

            InitializePlugin( plugin );

            _mainThread = new Thread( () =>
            {
                _window = new MainWindow();
                _window.ShowDialog();
            } ) { IsBackground = true };

            _mainThread.SetApartmentState( ApartmentState.STA );
            _mainThread.Start();
        }

        private static void ProcessUseObjectQueue( int serial )
        {
            SendPacketToServer( new UseObject( serial ) );
            Thread.Sleep( Options.CurrentOptions.ActionDelayMS );
        }

        internal static unsafe void InitializePlugin( PluginHeader* plugin )
        {
            _onConnected = OnConnected;
            _onDisconnected = OnDisconnected;
            _onReceive = OnPacketReceive;
            _onSend = OnPacketSend;
            _onPlayerPositionChanged = OnPlayerPositionChanged;
            _onClientClosing = OnClientClosing;
            _onHotkeyPressed = OnHotkeyPressed;
            _onMouse = OnMouse;

            plugin->OnConnected = Marshal.GetFunctionPointerForDelegate( _onConnected );
            plugin->OnDisconnected = Marshal.GetFunctionPointerForDelegate( _onDisconnected );
            plugin->OnRecv = Marshal.GetFunctionPointerForDelegate( _onReceive );
            plugin->OnSend = Marshal.GetFunctionPointerForDelegate( _onSend );
            plugin->OnPlayerPositionChanged = Marshal.GetFunctionPointerForDelegate( _onPlayerPositionChanged );
            plugin->OnClientClosing = Marshal.GetFunctionPointerForDelegate( _onClientClosing );
            plugin->OnHotkeyPressed = Marshal.GetFunctionPointerForDelegate( _onHotkeyPressed );
            plugin->OnMouse = Marshal.GetFunctionPointerForDelegate( _onMouse );

            _getPacketLength = Marshal.GetDelegateForFunctionPointer<OnGetPacketLength>( plugin->GetPacketLength );
            _getUOFilePath = Marshal.GetDelegateForFunctionPointer<OnGetUOFilePath>( plugin->GetUOFilePath );
            _sendToClient = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>( plugin->Recv );
            _sendToServer = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>( plugin->Send );
            _requestMove = Marshal.GetDelegateForFunctionPointer<RequestMove>( plugin->RequestMove );

            ClientPath = _getUOFilePath();

            if ( !Path.IsPathRooted( ClientPath ) )
            {
                ClientPath = Path.GetFullPath( ClientPath );
            }

            Art.Initialize( ClientPath );
            Hues.Initialize( ClientPath );
            Cliloc.Initialize( ClientPath );
            Skills.Initialize( ClientPath );
            Speech.Initialize( ClientPath );
            TileData.Initialize( ClientPath );
        }

        private static void OnMouse( int button, int wheel )
        {
            MouseOptions mouse = MouseOptions.None;

            if ( button > 0 )
            {
                mouse = (MouseOptions) ( button + 1 );
            }

            if ( wheel != 0 )
            {
                mouse = wheel < 0 ? MouseOptions.MouseWheelUp : MouseOptions.MouseWheelDown;
            }

            HotkeyManager.GetInstance().OnMouseAction( mouse );
        }

        private static bool OnHotkeyPressed( int key, int mod, bool pressed )
        {
            Key keys = SDLKeys.SDLKeyToKeys( key );

            bool pass = HotkeyManager.GetInstance().OnHotkeyPressed( keys );

            return !pass;
        }

        private static void OnClientClosing()
        {
            Options.Save( StartupPath );
        }

        private static void OnPlayerPositionChanged( int x, int y, int z )
        {
            if ( Player != null )
            {
                Player.X = x;
                Player.Y = y;
                Player.Z = z;
            }

            Items.RemoveByDistance( MAX_DISTANCE, x, y );
            Mobiles.RemoveByDistance( MAX_DISTANCE, x, y );
        }

        public static Item GetOrCreateItem( int serial, int containerSerial = -1 )
        {
            return Items.GetItem( serial ) ?? new Item( serial, containerSerial );
        }

        public static Mobile GetOrCreateMobile( int serial )
        {
            if ( Player?.Serial == serial )
            {
                return Player;
            }

            return Mobiles.GetMobile( serial, out Mobile mobile ) ? mobile : new Mobile( serial );
        }

        private static void Initialize()
        {
            StartupPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

            if ( StartupPath == null )
            {
                throw new InvalidOperationException();
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            PacketWaitEntries = new PacketWaitEntries();

            _incomingQueue = new ThreadQueue<Packet>( ProcessIncomingQueue );
            _outgoingQueue = new ThreadQueue<Packet>( ProcessOutgoingQueue );

            IncomingPacketHandlers.Initialize();
            OutgoingPacketHandlers.Initialize();

            OutgoingPacketFilters.Initialize();

            CommandsManager.Initialize();
        }

        private static void ProcessIncomingQueue( Packet packet )
        {
            PacketReceivedEvent?.Invoke( packet.GetPacket(), packet.GetLength() );

            PacketHandler handler = IncomingPacketHandlers.GetHandler( packet.GetPacketID() );

            int length = _getPacketLength( packet.GetPacketID() );

            handler?.OnReceive?.Invoke( new PacketReader( packet.GetPacket(), packet.GetLength(), length > 0 ) );

            PacketWaitEntries.CheckWait( packet.GetPacket(), PacketDirection.Incoming );
        }

        private static void ProcessOutgoingQueue( Packet packet )
        {
            PacketSentEvent?.Invoke( packet.GetPacket(), packet.GetLength() );

            PacketHandler handler = OutgoingPacketHandlers.GetHandler( packet.GetPacketID() );

            int length = _getPacketLength( packet.GetPacketID() );

            handler?.OnReceive?.Invoke( new PacketReader( packet.GetPacket(), packet.GetLength(), length > 0 ) );

            PacketWaitEntries.CheckWait( packet.GetPacket(), PacketDirection.Outgoing );
        }

        private static Assembly OnAssemblyResolve( object sender, ResolveEventArgs args )
        {
            string assemblyname = new AssemblyName( args.Name ).Name;

            string[] searchPaths = { StartupPath, RuntimeEnvironment.GetRuntimeDirectory() };

            if ( assemblyname.Contains( "Colletions" ) )
            {
                assemblyname = "System.Collections";
            }

            foreach ( string searchPath in searchPaths )
            {
                string fullPath = Path.Combine( searchPath, assemblyname + ".dll" );

                if ( !File.Exists( fullPath ) )
                {
                    continue;
                }

                Assembly assembly = Assembly.LoadFrom( fullPath );

                return assembly;
            }

            return null;
        }

        public static void SetPlayer( PlayerMobile mobile )
        {
            Player = mobile;

            PlayerInitializedEvent?.Invoke( mobile );

            mobile.MobileStatusUpdated += ( status, newStatus ) =>
            {
                if ( !Options.CurrentOptions.UseDeathScreenWhilstHidden )
                {
                    return;
                }

                if ( newStatus.HasFlag( MobileStatus.Hidden ) )
                {
                    SendPacketToClient( new MobileUpdate( mobile.Serial, mobile.ID == 0x191 ? 0x193 : 0x192, mobile.Hue,
                        newStatus, mobile.X,
                        mobile.Y, mobile.Z, mobile.Direction ) );
                }
            };

            Task.Run( async () =>
            {
                try
                {
                    GitHubClient client = new GitHubClient( new ProductHeaderValue( "ClassicAssist" ) );

                    IReadOnlyList<Release> releases =
                        await client.Repository.Release.GetAll( "Reetus",
                            "ClassicAssist" );

                    Release latestRelease = releases.FirstOrDefault();

                    if ( latestRelease == null )
                    {
                        return;
                    }

                    Version latestVersion = Version.Parse( latestRelease.TagName );

                    if ( !Version.TryParse(
                        FileVersionInfo.GetVersionInfo( Path.Combine( StartupPath, "ClassicAssist.dll" ) )
                            .ProductVersion,
                        out Version localVersion ) )
                    {
                        return;
                    }

                    if ( latestVersion > localVersion && Options.CurrentOptions.UpdateGumpVersion < latestVersion )
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine( Strings.ProductName );
                        message.AppendLine( $"{Strings.New_version_available_} {latestVersion}" );
                        message.AppendLine();

                        UpdateMessageGump gump = new UpdateMessageGump( message.ToString(), latestVersion );
                        byte[] packet = gump.Compile();

                        SendPacketToClient( packet, packet.Length );
                    }
                }
                catch ( Exception e )
                {
                    // Squash all
                }
            } );

            AbilitiesManager.GetInstance().Enabled = AbilityType.None;
        }

        public static void SendPacketToServer( byte[] packet, int length )
        {
            InternalPacketSentEvent?.Invoke( packet, length );

            _sendToServer?.Invoke( ref packet, ref length );
        }

        public static void SendPacketToClient( byte[] packet, int length )
        {
            InternalPacketReceivedEvent?.Invoke( packet, length );

            _sendToClient?.Invoke( ref packet, ref length );
        }

        public static void SendPacketToClient( PacketWriter packet )
        {
            byte[] data = packet.ToArray();

            SendPacketToClient( data, data.Length );
        }

        public static void SendPacketToClient( BasePacket basePacket )
        {
            if ( basePacket.Direction != PacketDirection.Any && basePacket.Direction != PacketDirection.Incoming )
            {
                throw new InvalidOperationException( "Send packet wrong direction." );
            }

            byte[] data = basePacket.ToArray();

            SendPacketToClient( data, data.Length );
        }

        public static void SendPacketToServer( PacketWriter packet )
        {
            byte[] data = packet.ToArray();

            SendPacketToServer( data, data.Length );
        }

        public static void SendPacketToServer( BasePacket basePacket )
        {
            if ( basePacket.Direction != PacketDirection.Any && basePacket.Direction != PacketDirection.Outgoing )
            {
                throw new InvalidOperationException( "Send packet wrong direction." );
            }

            byte[] data = basePacket.ToArray();

            if ( data == null )
            {
                return;
            }

            SendPacketToServer( data, data.Length );
        }

        public static bool Move( Direction direction, bool run )
        {
            return _requestMove?.Invoke( (int) direction, run ) ?? false;
        }

        #region ClassicUO Events

        private static bool OnPacketSend( ref byte[] data, ref int length )
        {
            bool filter = false;

            if ( CommandsManager.IsSpeechPacket( data[0] ) )
            {
                filter = CommandsManager.CheckCommand( data, length );
            }

            if ( _outgoingPacketFilter.MatchFilterAll( data, out PacketFilterInfo[] pfis ) > 0 )
            {
                foreach ( PacketFilterInfo pfi in pfis )
                {
                    pfi.Action?.Invoke( data, pfi );
                }

                SentPacketFilteredEvent?.Invoke( data, data.Length );

                return false;
            }

            if ( OutgoingPacketFilters.CheckPacket( data, data.Length ) )
            {
                SentPacketFilteredEvent?.Invoke( data, data.Length );

                return false;
            }

            _outgoingQueue.Enqueue( new Packet( data, length ) );

            return !filter;
        }

        private static bool OnPacketReceive( ref byte[] data, ref int length )
        {
            if ( _incomingPacketFilter.MatchFilterAll( data, out PacketFilterInfo[] pfis ) > 0 )
            {
                foreach ( PacketFilterInfo pfi in pfis )
                {
                    pfi.Action?.Invoke( data, pfi );
                }

                ReceivedPacketFilteredEvent?.Invoke( data, data.Length );

                return false;
            }

            _incomingQueue.Enqueue( new Packet( data, length ) );

            return true;
        }

        public static Direction GetSequence( int sequence )
        {
            return (Direction) Thread.VolatileRead( ref _sequenceList[sequence] );
        }

        public static void SetSequence( int sequence, Direction direction )
        {
            _sequenceList[sequence] = (int) direction;
        }

        private static void OnConnected()
        {
            Connected = true;

            ConnectedEvent?.Invoke();
        }

        private static void OnDisconnected()
        {
            Connected = false;

            Items.Clear();
            Mobiles.Clear();
            Player = null;

            DisconnectedEvent?.Invoke();
        }

        #endregion

        #region Filters

        public static void AddSendFilter( PacketFilterInfo pfi )
        {
            _outgoingPacketFilter.Add( pfi );
        }

        public static void AddReceiveFilter( PacketFilterInfo pfi )
        {
            _incomingPacketFilter.Add( pfi );
        }

        public static void RemoveReceiveFilter( PacketFilterInfo pfi )
        {
            _incomingPacketFilter.Remove( pfi );
        }

        public static void RemoveSendFilter( PacketFilterInfo pfi )
        {
            _outgoingPacketFilter.Remove( pfi );
        }

        public static void ClearSendFilter()
        {
            _outgoingPacketFilter?.Clear();
        }

        public static void ClearReceiveFilter()
        {
            _incomingPacketFilter?.Clear();
        }

        #endregion
    }
}