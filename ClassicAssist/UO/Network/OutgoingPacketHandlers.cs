﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assistant;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network.PacketFilter;

namespace ClassicAssist.UO.Network
{
    public static class OutgoingPacketHandlers
    {
        public delegate void dTargetSentEvent( TargetType targetType, int senderSerial, int flags, int serial, int x,
            int y, int z, int id );

        private static PacketHandler[] _handlers;
        private static PacketHandler[] _extendedHandlers;
        public static event dTargetSentEvent TargetSentEvent;

        public static void Initialize()
        {
            _handlers = new PacketHandler[0x100];
            _extendedHandlers = new PacketHandler[0x100];

            Register(0x6C, 19, OnTargetSent);
            Register(0xB1, 0, OnGumpButtonPressed);
            Register( 0xEF, 31, OnNewClientVersion );
        }

        private static void OnGumpButtonPressed( PacketReader reader )
        {
            int senderSerial = reader.ReadInt32();
            int gumpId = reader.ReadInt32();

            Engine.GumpList.TryRemove( gumpId, out _ );
        }

        private static void OnTargetSent( PacketReader reader )
        {
            if ( Engine.Player == null )
            {
                return;
            }

            TargetType targetType = (TargetType) reader.ReadByte();

            Engine.Player.LastTargetType = targetType;

            int senderSerial = reader.ReadInt32(); // sender serial
            int flags = reader.ReadByte();
            int serial = reader.ReadInt32();
            int x = reader.ReadInt16();
            int y = reader.ReadInt16();
            int z = reader.ReadInt16();
            int id = reader.ReadInt16();

            if ( targetType == TargetType.Object && flags != 0x03 )
            {
                Engine.Player.LastTargetSerial = serial;
            }

            TargetSentEvent?.Invoke( targetType, senderSerial, flags, serial, x, y, z, id );
        }

        private static void OnNewClientVersion( PacketReader reader )
        {
            reader.ReadInt32();
            Engine.ClientVersion = new Version( reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(),
                reader.ReadInt32() );
        }

        private static void Register( int packetId, int length, OnPacketReceive onReceive )
        {
            _handlers[packetId] = new PacketHandler( packetId, length, onReceive );
        }

        private static void RegisterExtended( int packetId, int length, OnPacketReceive onReceive )
        {
            _handlers[packetId] = new PacketHandler( packetId, length, onReceive );
        }

        internal static PacketHandler GetHandler( int packetId )
        {
            return _handlers[packetId];
        }

        private static PacketHandler GetExtendedHandler( int packetId )
        {
            return _handlers[packetId];
        }
    }
}