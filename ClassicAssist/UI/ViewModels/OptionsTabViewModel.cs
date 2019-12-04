﻿using ClassicAssist.Data;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.Misc;
using Newtonsoft.Json.Linq;

namespace ClassicAssist.UI.ViewModels
{
    public class OptionsTabViewModel : BaseViewModel, ISettingProvider
    {
        private Options _options;

        public Options Options
        {
            get => _options;
            set => SetProperty( ref _options, value );
        }

        public void Serialize( JObject json )
        {
            JObject options = new JObject();

            JObject useOnce = new JObject { ["Persist"] = Options.PersistUseOnce };

            if ( Options.PersistUseOnce )
            {
                JArray useOnceItems = new JArray();

                foreach ( int serial in ActionCommands.UseOnceList )
                {
                    useOnceItems.Add( serial );
                }

                useOnce.Add( "Items", useOnceItems );
            }

            options.Add( "UseOnce", useOnce );
            options.Add( "UseDeathScreenWhilstHidden", Options.UseDeathScreenWhilstHidden );
            options.Add( "CommandPrefix", Options.CommandPrefix );

            json.Add( "Options", options );
        }

        public void Deserialize( JObject json, Options options )
        {
            Options = options;

            JToken config = json["Options"];

            if ( config?["UseOnce"] != null )
            {
                Options.PersistUseOnce = config["UseOnce"]["Persist"]?.ToObject<bool>() ?? false;

                if ( Options.PersistUseOnce )
                {
                    foreach ( JToken token in config["UseOnce"]["Items"] )
                    {
                        ActionCommands.UseOnceList.Add( token.ToObject<int>() );
                    }
                }
            }

            Options.UseDeathScreenWhilstHidden = config?["UseDeathScreenWhilstHidden"]?.ToObject<bool>() ?? false;
            Options.CommandPrefix = config?["CommandPrefix"]?.ToObject<char>() ?? '=';
        }
    }
}