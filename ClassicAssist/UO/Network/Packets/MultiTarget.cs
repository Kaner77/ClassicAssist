﻿#region License
// Copyright (C) 2020 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicAssist.UO.Network.PacketFilter;

namespace ClassicAssist.UO.Network.Packets
{
    public class MultiTarget : BasePacket, IMacroCommandParser
    {
        public MultiTarget()
        {
            //TODO?
        }

        public string Parse( byte[] packet, int length, PacketDirection direction )
        {
            if ( packet[0] != 0x99 )
            {
                return null;
            }

            return direction == PacketDirection.Incoming ? "WaitForTarget(5000)\r\n" : null;
        }
    }
}