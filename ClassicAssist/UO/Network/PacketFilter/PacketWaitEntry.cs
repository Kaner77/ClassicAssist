﻿using System.Threading;

namespace ClassicAssist.UO.Network.PacketFilter
{
    public class PacketWaitEntry
    {
        public bool AutoRemove { get; set; }
        public AutoResetEvent Lock { get; set; }
        public byte[] Packet { get; set; }
        public PacketDirection PacketDirection { get; set; }
        public PacketFilterInfo PFI { get; set; }
    }
}