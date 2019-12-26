﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Serialization;
using ClassicAssist.Annotations;
using ClassicAssist.Misc;

namespace ClassicAssist.Data.Hotkeys
{
    public class HotkeySettable : INotifyPropertyChanged
    {
        public delegate void HotkeyChangedEventHandler( object sender, HotkeyChangedEventArgs e );

        private ShortcutKeys _hotkey = new ShortcutKeys();

        private string _name;
        private bool _passToUo = true;

        public Action<HotkeySettable> Action { get; set; }
        public virtual bool Disableable { get; set; } = true;

        public ShortcutKeys Hotkey
        {
            get => _hotkey;
            set
            {
                if ( !Equals( value, ShortcutKeys.Default ) )
                {
                    HotkeyManager manager = HotkeyManager.GetInstance();
                    manager.ClearPreviousHotkey( value );
                }

                SetProperty( ref _hotkey, value );
                HotkeyChanged?.Invoke( this, new HotkeyChangedEventArgs( _hotkey, value ) );
            }
        }

        [XmlIgnore]
        public ImageSource Image =>
            Equals( Hotkey, ShortcutKeys.Default )
                ? Properties.Resources.red_circle.ToImageSource()
                : Properties.Resources.green_circle.ToImageSource();

        public string Name
        {
            get => _name;
            set => SetProperty( ref _name, value );
        }

        public bool PassToUO
        {
            get => _passToUo;
            set => SetProperty( ref _passToUo, value );
        }

        public event HotkeyChangedEventHandler HotkeyChanged;

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        // ReSharper disable once RedundantAssignment
        public void SetProperty<T>( ref T field, T value, [CallerMemberName] string propertyName = null )
        {
            field = value;
            OnPropertyChanged( propertyName );
        }

        #endregion
    }
}