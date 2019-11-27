﻿using System;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.UI.Misc;

namespace ClassicAssist.Data.Organizer
{
    public class OrganizerEntry : HotkeySettable
    {
        private ObservableCollectionEx<OrganizerItem> _items = new ObservableCollectionEx<OrganizerItem>();
        private bool _stack = true;
        public int SourceContainer { get; set; }
        public int DestinationContainer { get; set; }

        public ObservableCollectionEx<OrganizerItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public bool Stack
        {
            get => _stack;
            set => SetProperty(ref _stack, value);
        }

        public Func<bool> IsRunning;

        public override string ToString()
        {
            return Name;
        }
    }
}