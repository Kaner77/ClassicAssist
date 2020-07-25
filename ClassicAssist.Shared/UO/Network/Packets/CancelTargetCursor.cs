﻿using ClassicAssist.UO.Data;

namespace ClassicAssist.UO.Network.Packets
{
    public class CancelTargetCursor : BasePacket
    {
        public CancelTargetCursor( uint senderSerial )
        {
            _writer = new PacketWriter( 19 );
            _writer.Write( (byte) 0x6C );
            _writer.Write( (byte) 0 );
            _writer.Write( senderSerial );
            _writer.Write( (byte) 3 );
            _writer.Fill();
        }

        public CancelTargetCursor( int senderSerial )
        {
            _writer = new PacketWriter( 19 );
            _writer.Write( (byte) 0x6C );
            _writer.Write( (byte) 0 );
            _writer.Write( senderSerial );
            _writer.Write( (byte) 3 );
            _writer.Fill();
        }
    }
}