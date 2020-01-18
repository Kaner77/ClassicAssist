﻿using Assistant;
using ClassicAssist.Data.Regions;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UO.Objects;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class RegionCommands
    {
        [CommandsDisplay( Category = "Actions",
            Description = "Returns true if the region of the target has the specified attribute.",
            InsertText = "if InRegion(\"Guarded\", \"self\")" )]
        public static bool InRegion( string attribute, object obj )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            Entity entity = (Entity) Engine.Items.GetItem( serial ) ?? Engine.Mobiles.GetMobile( serial );

            if ( entity == null )
            {
                UOC.SystemMessage( Strings.Cannot_find_item___ );
                return false;
            }

            RegionAttributes attr = Utility.GetEnumValueByName<RegionAttributes>( attribute );

            Region region = entity.GetRegion();

            return region != null && region.Attributes.HasFlag( attr );
        }
    }
}