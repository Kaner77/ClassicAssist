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

namespace ClassicAssist.UO.Objects
{
    public class Menu
    {
        public int ID { get; set; }
        public MenuEntry[] Lines { get; set; }
        public int Serial { get; set; }
        public string Title { get; set; }
    }

    public class MenuEntry
    {
        public int Hue { get; set; }
        public int ID { get; set; }
        public int Index { get; set; }
        public string Title { get; set; }
    }
}