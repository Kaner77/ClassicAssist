﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Data.Autoloot;
using ClassicAssist.Data.Regions;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UI.Misc;
using ClassicAssist.UI.Views;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network;
using ClassicAssist.UO.Network.PacketFilter;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.UI.ViewModels.Agents
{
    public class AutolootViewModel : BaseViewModel, ISettingProvider
    {
        private readonly object _autolootLock = new object();
        private ICommand _clipboardCopyCommand;
        private ICommand _clipboardPasteCommand;

        private ObservableCollection<PropertyEntry>
            _constraints = new ObservableCollection<PropertyEntry>();

        private int _containerSerial;

        private bool _disableInGuardzone;
        private bool _enabled;

        private ICommand _insertCommand;
        private ICommand _insertConstraintCommand;

        private ObservableCollectionEx<AutolootEntry> _items = new ObservableCollectionEx<AutolootEntry>();
        private ICommand _removeCommand;
        private ICommand _removeConstraintCommand;
        private AutolootEntry _selectedItem;
        private AutolootConstraintEntry _selectedProperty;
        private ICommand _selectHueCommand;
        private ICommand _setContainerCommand;

        public AutolootViewModel()
        {
            string constraintsFile = Path.Combine( Engine.StartupPath ?? Environment.CurrentDirectory, "Data",
                "Properties.json" );

            if ( !File.Exists( constraintsFile ) )
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            using ( StreamReader sr = new StreamReader( constraintsFile ) )
            {
                using ( JsonTextReader reader = new JsonTextReader( sr ) )
                {
                    PropertyEntry[] constraints = serializer.Deserialize<PropertyEntry[]>( reader );

                    foreach ( PropertyEntry constraint in constraints )
                    {
                        Constraints.AddSorted( constraint );
                    }
                }
            }

            IncomingPacketHandlers.CorpseContainerDisplayEvent += OnCorpseContainerDisplayEvent;
        }

        public ICommand ClipboardCopyCommand =>
            _clipboardCopyCommand ?? ( _clipboardCopyCommand = new RelayCommand( ClipboardCopy, o => true ) );

        public ICommand ClipboardPasteCommand =>
            _clipboardPasteCommand ?? ( _clipboardPasteCommand = new RelayCommand( ClipboardPaste, o => true ) );

        public ObservableCollection<PropertyEntry> Constraints
        {
            get => _constraints;
            set => SetProperty( ref _constraints, value );
        }

        public int ContainerSerial
        {
            get => _containerSerial;
            set => SetProperty( ref _containerSerial, value );
        }

        public bool DisableInGuardzone
        {
            get => _disableInGuardzone;
            set => SetProperty( ref _disableInGuardzone, value );
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty( ref _enabled, value );
        }

        public ICommand InsertCommand =>
            _insertCommand ?? ( _insertCommand = new RelayCommandAsync( Insert, o => Engine.Connected ) );

        public ICommand InsertConstraintCommand =>
            _insertConstraintCommand ?? ( _insertConstraintCommand =
                new RelayCommand( InsertConstraint, o => SelectedItem != null ) );

        public ObservableCollectionEx<AutolootEntry> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ICommand RemoveCommand =>
            _removeCommand ?? ( _removeCommand = new RelayCommandAsync( Remove, o => SelectedItem != null ) );

        public ICommand RemoveConstraintCommand =>
            _removeConstraintCommand ?? ( _removeConstraintCommand =
                new RelayCommand( RemoveConstraint, o => SelectedProperty != null ) );

        public AutolootEntry SelectedItem
        {
            get => _selectedItem;
            set => SetProperty( ref _selectedItem, value );
        }

        public AutolootConstraintEntry SelectedProperty
        {
            get => _selectedProperty;
            set => SetProperty( ref _selectedProperty, value );
        }

        public ICommand SelectHueCommand =>
            _selectHueCommand ?? ( _selectHueCommand = new RelayCommand( SelectHue, o => SelectedItem != null ) );

        public ICommand SetContainerCommand =>
            _setContainerCommand ?? ( _setContainerCommand = new RelayCommandAsync( SetContainer, o => true ) );

        public void Serialize( JObject json )
        {
            if ( json == null )
            {
                return;
            }

            JObject autolootObj = new JObject
            {
                { "Enabled", Enabled },
                { "DisableInGuardzone", DisableInGuardzone },
                { "Container", ContainerSerial }
            };

            JArray itemsArray = new JArray();

            foreach ( AutolootEntry entry in Items )
            {
                JObject entryObj = new JObject
                {
                    { "Name", entry.Name },
                    { "ID", entry.ID },
                    { "Autoloot", entry.Autoloot },
                    { "Rehue", entry.Rehue },
                    { "RehueHue", entry.RehueHue }
                };

                if ( entry.Constraints != null )
                {
                    JArray constraintsArray = new JArray();

                    foreach ( AutolootConstraintEntry constraint in entry.Constraints )
                    {
                        JObject constraintObj = new JObject
                        {
                            { "Name", constraint.Property.Name },
                            { "Operator", constraint.Operator.ToString() },
                            { "Value", constraint.Value }
                        };

                        constraintsArray.Add( constraintObj );
                    }

                    entryObj.Add( "Properties", constraintsArray );
                }

                itemsArray.Add( entryObj );
            }

            autolootObj.Add( "Items", itemsArray );

            json.Add( "Autoloot", autolootObj );
        }

        public void Deserialize( JObject json, Options options )
        {
            Items.Clear();

            if ( json?["Autoloot"] == null )
            {
                return;
            }

            JToken config = json["Autoloot"];

            Enabled = config["Enabled"]?.ToObject<bool>() ?? true;
            DisableInGuardzone = config["DisableInGuardzone"]?.ToObject<bool>() ?? false;
            ContainerSerial = config["Container"]?.ToObject<int>() ?? 0;

            if ( config["Items"] != null )
            {
                JToken items = config["Items"];

                foreach ( JToken token in items )
                {
                    AutolootEntry entry = new AutolootEntry
                    {
                        Name = token["Name"]?.ToObject<string>() ?? "Unknown",
                        ID = token["ID"]?.ToObject<int>() ?? 0,
                        Autoloot = token["Autoloot"]?.ToObject<bool>() ?? false,
                        Rehue = token["Rehue"]?.ToObject<bool>() ?? false,
                        RehueHue = token["RehueHue"]?.ToObject<int>() ?? 0
                    };

                    if ( token["Properties"] != null )
                    {
                        List<AutolootConstraintEntry> constraintsList = new List<AutolootConstraintEntry>();

                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach ( JToken constraintToken in token["Properties"] )
                        {
                            string constraintName = constraintToken["Name"]?.ToObject<string>() ?? "Unknown";

                            PropertyEntry propertyEntry = Constraints.FirstOrDefault( c => c.Name == constraintName );

                            if ( propertyEntry == null )
                            {
                                continue;
                            }

                            AutolootConstraintEntry constraintObj = new AutolootConstraintEntry
                            {
                                Property = propertyEntry,
                                Operator = constraintToken["Operator"]?.ToObject<AutolootOperator>() ??
                                           AutolootOperator.Equal,
                                Value = constraintToken["Value"]?.ToObject<int>() ?? 0
                            };

                            constraintsList.Add( constraintObj );
                        }

                        entry.Constraints.AddRange( constraintsList );
                    }

                    Items.Add( entry );
                }
            }
        }

        private void ClipboardPaste( object obj )
        {
            string text = Clipboard.GetText();

            try
            {
                AutolootConstraintEntry entry = JsonConvert.DeserializeObject<AutolootConstraintEntry>( text );

                if ( entry != null )
                {
                    SelectedItem?.Constraints.Add( entry );
                }
            }
            catch ( Exception )
            {
                // ignored
            }
        }

        private static void ClipboardCopy( object obj )
        {
            if ( !( obj is AutolootConstraintEntry entry ) )
            {
                return;
            }

            string text = JsonConvert.SerializeObject( entry );

            Clipboard.SetText( text );
        }

        internal void OnCorpseContainerDisplayEvent( int serial )
        {
            if ( !Enabled )
            {
                return;
            }

            lock ( _autolootLock )
            {
                Item item = Engine.Items.GetItem( serial );

                if ( item == null || item.ID != 0x2006 )
                {
                    return;
                }

                PacketWaitEntry we = Engine.PacketWaitEntries.Add( new PacketFilterInfo( 0x3C,
                        new[] { PacketFilterConditions.IntAtPositionCondition( serial, 19 ) } ),
                    PacketDirection.Incoming );

                bool result = we.Lock.WaitOne( 5000 );

                if ( !result )
                {
                    return;
                }

                IEnumerable<Item> items = Engine.Items.GetItem( serial )?.Container.GetItems();

                if ( items == null )
                {
                    return;
                }

                List<Item> lootItems = new List<Item>();

                foreach ( AutolootEntry entry in Items )
                {
                    IEnumerable<Item> matchItems = AutolootFilter( items, entry );

                    if ( matchItems == null )
                    {
                        continue;
                    }

                    foreach ( Item matchItem in matchItems )
                    {
                        if ( entry.Rehue )
                        {
                            Engine.SendPacketToClient( new ContainerContentUpdate( matchItem.Serial, matchItem.ID,
                                matchItem.Direction, matchItem.Count,
                                matchItem.X, matchItem.Y, matchItem.Grid, matchItem.Owner, entry.RehueHue ) );
                        }

                        if ( DisableInGuardzone &&
                             Engine.Player.GetRegion().Attributes.HasFlag( RegionAttributes.Guarded ) )
                        {
                            continue;
                        }

                        if ( entry.Autoloot )
                        {
                            lootItems.Add( matchItem );
                        }
                    }
                }

                foreach ( Item lootItem in lootItems )
                {
                    int containerSerial = ContainerSerial;

                    if ( containerSerial == 0 )
                    {
                        if ( Engine.Player.Backpack == null )
                        {
                            return;
                        }

                        containerSerial = Engine.Player.Backpack.Serial;
                    }

                    Thread.Sleep( Options.CurrentOptions.ActionDelayMS );
                    UOC.SystemMessage( string.Format( Strings.Autolooting___0__, lootItem.Name ) );
                    UOC.DragDropAsync( lootItem.Serial, lootItem.Count, containerSerial ).Wait();
                }
            }
        }

        private void RemoveConstraint( object obj )
        {
            if ( !( obj is AutolootConstraintEntry constraint ) )
            {
                return;
            }

            SelectedItem.Constraints.Remove( constraint );
        }

        private void InsertConstraint( object obj )
        {
            if ( !( obj is PropertyEntry propertyEntry ) )
            {
                return;
            }

            List<AutolootConstraintEntry> constraints =
                new List<AutolootConstraintEntry>( SelectedItem.Constraints )
                {
                    new AutolootConstraintEntry { Property = propertyEntry }
                };

            SelectedItem.Constraints = new ObservableCollection<AutolootConstraintEntry>( constraints );
        }

        private async Task SetContainer( object arg )
        {
            int serial = await UOC.GetTargeSerialAsync( Strings.Target_container___ );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            ContainerSerial = serial;
        }

        private async Task Insert( object arg )
        {
            int serial = await UOC.GetTargeSerialAsync( Strings.Target_object___ );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return;
            }

            Item item = Engine.Items.GetItem( serial );

            if ( item == null )
            {
                UOC.SystemMessage( Strings.Cannot_find_item___ );
                return;
            }

            Items.Add( new AutolootEntry
            {
                Name = TileData.GetStaticTile( item.ID ).Name,
                ID = item.ID,
                Constraints = new ObservableCollection<AutolootConstraintEntry>()
            } );
        }

        private async Task Remove( object arg )
        {
            if ( !( arg is AutolootEntry entry ) )
            {
                return;
            }

            Items.Remove( entry );

            await Task.CompletedTask;
        }

        private static void SelectHue( object obj )
        {
            if ( !( obj is AutolootEntry entry ) )
            {
                return;
            }

            if ( HuePickerWindow.GetHue( out int hue ) )
            {
                entry.RehueHue = hue;
            }
        }

        public static IEnumerable<Item> AutolootFilter( IEnumerable<Item> items, AutolootEntry entry )
        {
            return items == null
                ? null
                : ( from item in items
                    where item.ID == entry.ID
                    let predicates = ConstraintsToPredicates( entry.Constraints )
                    where !predicates.Any() || CheckPredicates( item, predicates )
                    select item ).ToList();
        }

        private static bool CheckPredicates( Item item, IEnumerable<Predicate<Item>> predicates )
        {
            return predicates.All( predicate => predicate( item ) );
        }

        public static IEnumerable<Predicate<Item>> ConstraintsToPredicates(
            IEnumerable<AutolootConstraintEntry> constraints )
        {
            List<Predicate<Item>> predicates = new List<Predicate<Item>>();

            foreach ( AutolootConstraintEntry constraint in constraints )
            {
                switch ( constraint.Property.ConstraintType )
                {
                    case PropertyType.Properties:
                        predicates.Add( i => i.Properties != null && constraint.Property.Clilocs.Any( cliloc =>
                                                 i.Properties.Any( p => AutolootHelpers.MatchProperty( p, cliloc,
                                                     constraint.Property, constraint.Operator,
                                                     constraint.Value ) ) ) );
                        break;
                    case PropertyType.Object:

                        predicates.Add( i =>
                            AutolootHelpers.ItemHasObjectProperty( i, constraint.Property.Name ) &&
                            AutolootHelpers.Operation(
                                constraint.Operator,
                                AutolootHelpers.GetItemObjectPropertyValue<int>( i, constraint.Property.Name ),
                                constraint.Value ) );

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return predicates;
        }
    }
}