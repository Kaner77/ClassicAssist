﻿using System;
using System.Linq;
using Assistant;
using ClassicAssist.Resources;
using ClassicAssist.UO.Network.PacketFilter;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class PropertiesCommands
    {
        [CommandsDisplay( Category = "Properties",
            Description = "Wait for item properties to be received for specified item.",
            InsertText = "WaitForProperties(\"backpack\")" )]
        public static bool WaitForProperties( object obj, int timeout )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            PacketFilterInfo pfi = new PacketFilterInfo( 0xD6,
                new[] { PacketFilterConditions.IntAtPositionCondition( serial, 5 ) } );

            PacketWaitEntry we = Engine.PacketWaitEntries.Add( pfi, PacketDirection.Incoming, true );

            Engine.SendPacketToServer( new BatchQueryProperties( serial ) );

            try
            {
                bool result = we.Lock.WaitOne( timeout );

                return result;
            }
            finally
            {
                Engine.PacketWaitEntries.Remove( we );
            }
        }

        [CommandsDisplay( Category = "Properties",
            Description = "Returns true if the given text appears in the items item properties.",
            InsertText = "if Property(\"item\", \"Defense Chance Increase\")" )]
        public static bool Property( object obj, string value )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            Entity entity = (Entity) Engine.Items.GetItem( serial ) ?? Engine.Mobiles.GetMobile( serial );

            if ( entity.Properties != null )
            {
                return entity.Properties.Any( pe => pe.Text.ToLower().Contains( value.ToLower() ) );
            }

            UOC.SystemMessage( Strings.Item_properties_null_or_not_loaded___ );
            return false;
        }

        [CommandsDisplay( Category = "Properties",
            Description = "Returns the argument value of the given property name. Optional argument index.",
            InsertText = "val = PropertyValue[int](\"backpack\", \"Contents\")" )]
        public static T PropertyValue<T>( object obj, string property, int argument = 0 )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return default;
            }

            Entity entity = (Entity) Engine.Items.GetItem( serial ) ?? Engine.Mobiles.GetMobile( serial );

            if ( entity.Properties != null )
            {
                Property p = entity.Properties.FirstOrDefault( pe => pe.Text.ToLower().Contains( property.ToLower() ) );

                return (T) Convert.ChangeType( p?.Arguments[argument], typeof( T ) );
            }

            UOC.SystemMessage( Strings.Item_properties_null_or_not_loaded___ );
            return default;
        }
    }
}