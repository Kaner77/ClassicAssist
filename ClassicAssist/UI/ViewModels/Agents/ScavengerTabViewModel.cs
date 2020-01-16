﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Data.Scavenger;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Objects;
using Newtonsoft.Json.Linq;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.UI.ViewModels.Agents
{
    public class ScavengerTabViewModel : BaseViewModel, ISettingProvider
    {
        private readonly List<int> _ignoreList;
        private readonly object _scavengeLock = new object();
        private ICommand _clearAllCommand;
        private int _containerSerial;
        private bool _enabled;
        private ICommand _insertCommand;
        private ObservableCollection<ScavengerEntry> _items = new ObservableCollection<ScavengerEntry>();
        private ICommand _removeCommand;
        private ScavengerEntry _selectedItem;
        private ICommand _setContainerCommand;

        public ScavengerTabViewModel()
        {
            ScavengerManager manager = ScavengerManager.GetInstance();
            manager.Items = Items;
            manager.CheckArea = () => Task.Run( CheckArea );
            _ignoreList = new List<int>();
        }

        public ICommand ClearAllCommand =>
            _clearAllCommand ?? ( _clearAllCommand = new RelayCommandAsync( ClearAll, o => Items.Count > 0 ) );

        public int ContainerSerial
        {
            get => _containerSerial;
            set => SetProperty( ref _containerSerial, value );
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty( ref _enabled, value );
        }

        public ICommand InsertCommand =>
            _insertCommand ?? ( _insertCommand = new RelayCommandAsync( Insert, o => true ) );

        public ObservableCollection<ScavengerEntry> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ICommand RemoveCommand =>
            _removeCommand ?? ( _removeCommand = new RelayCommandAsync( Remove, o => SelectedItem != null ) );

        public ScavengerEntry SelectedItem
        {
            get => _selectedItem;
            set => SetProperty( ref _selectedItem, value );
        }

        public ICommand SetContainerCommand =>
            _setContainerCommand ?? ( _setContainerCommand = new RelayCommandAsync( SetContainer, o => true ) );

        public void Serialize( JObject json )
        {
            if ( json == null )
            {
                return;
            }

            JObject scavengerObj = new JObject { { "Enabled", Enabled }, { "Container", ContainerSerial } };

            JArray itemsArray = new JArray();

            foreach ( ScavengerEntry entry in Items )
            {
                itemsArray.Add( new JObject
                {
                    { "Graphic", entry.Graphic },
                    { "Name", entry.Name },
                    { "Hue", entry.Hue },
                    { "Enabled", entry.Enabled }
                } );
            }

            scavengerObj.Add( "Items", itemsArray );

            json.Add( "Scavenger", scavengerObj );
        }

        public void Deserialize( JObject json, Options options )
        {
            if ( json?["Scavenger"] == null )
            {
                return;
            }

            JToken config = json["Scavenger"];

            Enabled = config["Enabled"]?.ToObject<bool>() ?? true;
            ContainerSerial = config["Container"]?.ToObject<int>() ?? 0;

            if ( config["Items"] == null )
            {
                return;
            }

            foreach ( JToken token in config["Items"] )
            {
                ScavengerEntry entry = new ScavengerEntry
                {
                    Graphic = token["Graphic"]?.ToObject<int>() ?? 0,
                    Name = token["Name"]?.ToObject<string>() ?? "Unknown",
                    Hue = token["Hue"]?.ToObject<int>() ?? 0,
                    Enabled = token["Enabled"]?.ToObject<bool>() ?? true
                };

                Items.Add( entry );
            }
        }

        private void CheckArea()
        {
            if ( !Enabled || Engine.Player == null )
            {
                return;
            }

            List<Item> scavengerItems = new List<Item>();

            if ( Engine.Player.WeightMax - Engine.Player.Weight <= 50 )
            {
                return;
            }

            lock ( _scavengeLock )
            {
                foreach ( ScavengerEntry entry in Items )
                {
                    Item[] matches = Engine.Items.SelectEntities( i =>
                        i.Distance <= 2 && i.Owner == 0 && i.ID == entry.Graphic && i.Hue == entry.Hue &&
                        !_ignoreList.Contains( i.Serial ) );

                    if ( matches == null )
                    {
                        continue;
                    }

                    scavengerItems.AddRange( matches );
                }

                if ( scavengerItems.Count == 0 )
                {
                    return;
                }

                Item container = Engine.Items.GetItem( _containerSerial ) ?? Engine.Player.Backpack;

                if ( container == null )
                {
                    return;
                }

                foreach ( Item scavengerItem in scavengerItems )
                {
                    _ignoreList.Add( scavengerItem.Serial );
                    UOC.DragDropAsync( scavengerItem.Serial, scavengerItem.Count, container.Serial ).Wait();
                }
            }
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

            string tiledataName = TileData.GetStaticTile( item.ID ).Name ?? "Unknown";

            ScavengerEntry entry =
                new ScavengerEntry { Enabled = true, Graphic = item.ID, Hue = item.Hue, Name = tiledataName };

            Items.Add( entry );
        }

        private async Task Remove( object arg )
        {
            if ( !( arg is ScavengerEntry entry ) )
            {
                return;
            }

            Items.Remove( entry );

            await Task.CompletedTask;
        }

        private async Task ClearAll( object arg )
        {
            Items.Clear();

            await Task.CompletedTask;
        }

        private async Task SetContainer( object arg )
        {
            int serial = await UOC.GetTargeSerialAsync( Strings.Select_destination_container___ );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_container___ );
                return;
            }

            ContainerSerial = serial;
        }
    }
}