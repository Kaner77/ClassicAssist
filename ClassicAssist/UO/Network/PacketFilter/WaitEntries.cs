﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClassicAssist.UO.Network.PacketFilter
{
    public class WaitEntries
    {
        private readonly List<WaitEntry> _waitEntries = new List<WaitEntry>();
        private readonly object _waitEntryLock = new object();

        public WaitEntry AddWait(PacketFilterInfo pfi, PacketDirection direction, bool autoRemove = false)
        {
            WaitEntry we = new WaitEntry
            {
                PFI = pfi,
                Lock = new AutoResetEvent(false),
                PacketDirection = direction,
                AutoRemove = autoRemove
            };

            lock (_waitEntryLock)
            {
                _waitEntries.Add(we);
            }

            return we;
        }

        public bool CheckWait(byte[] packet, PacketDirection direction)
        {
            lock (_waitEntryLock)
            {
                if (_waitEntries.Count == 0)
                    return false;
            }

            List<WaitEntry> matchedEntries = new List<WaitEntry>();

            lock (_waitEntryLock)
            {
                foreach ( WaitEntry t in _waitEntries.Where( t => packet[0] == t.PFI.PacketID ).Where( t => direction == t.PacketDirection ) )
                {
                    if (t.PFI.GetConditions() == null)
                    {
                        // No condition so just match packetid
                        matchedEntries.Add(t);
                    }
                    else
                    {
                        bool result = false;

                        foreach (PacketFilterCondition fc in t.PFI.GetConditions())
                        {
                            if (fc.Position + fc.Length > packet.Length)
                            {
                                result = false;

                                break;
                            }

                            byte[] tmp = new byte[fc.Length];
                            Buffer.BlockCopy(packet, fc.Position, tmp, 0, fc.Length);

                            if (!tmp.SequenceEqual(fc.GetBytes()))
                            {
                                result = false;

                                break;
                            }

                            result = true;
                        }

                        if (result)
                            matchedEntries.Add(t);
                    }
                }
            }

            if (matchedEntries.Count == 0)
                return false;

            List<WaitEntry> removeList = new List<WaitEntry>();

            foreach (WaitEntry entry in matchedEntries)
            {
                entry.Packet = new byte[packet.Length];
                Buffer.BlockCopy(packet, 0, entry.Packet, 0, packet.Length);
                entry.Lock.Set();
                entry.PFI.Action?.Invoke(packet, entry.PFI);

                if ( entry.AutoRemove )
                    removeList.Add( entry );
            }

            removeList.ForEach( RemoveWait );

            return true;
        }

        public void ClearWait()
        {
            lock (_waitEntryLock)
            {
                foreach (WaitEntry we in _waitEntries)
                {
                    we.Lock.Set();
                }

                _waitEntries?.Clear();
            }
        }

        public WaitEntry[] GetEntries()
        {
            lock ( _waitEntryLock )
            {
                return _waitEntries?.ToArray();
            }
        }

        public void RemoveWait(WaitEntry we)
        {
            lock (_waitEntryLock)
            {
                _waitEntries.Remove(we);
            }
        }
    }

    public class WaitEntry
    {
        public AutoResetEvent Lock { get; set; }
        public byte[] Packet { get; set; }
        public PacketDirection PacketDirection { get; set; }
        public PacketFilterInfo PFI { get; set; }
        public bool AutoRemove { get; set; }
    }

    public enum PacketDirection : byte
    {
        Incoming,
        Outgoing
    }
}