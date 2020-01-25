﻿using Assistant;
using ClassicAssist.Misc;
using ClassicAssist.Resources;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Network.PacketFilter;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class MovementCommands
    {
        private const int MOVEMENT_TIMEOUT = 500;
        private static bool _forceWalk;

        [CommandsDisplay( Category = "Movement", Description = "Walk in the given direction.",
            InsertText = "Walk(\"east\")" )]
        public static bool Walk( string direction )
        {
            return Move( direction, false );
        }

        [CommandsDisplay( Category = "Movement", Description = "Set force walk, True or False",
            InsertText = "SetForceWalk(True)" )]
        public static void SetForceWalk( bool force )
        {
            UOC.SetForceWalk( force );
            UOC.SystemMessage( force ? Strings.Force_Walk_On : Strings.Force_Walk_Off );
        }

        [CommandsDisplay( Category = "Movement", Description = "Toggle Force Walk", InsertText = "ToggleForceWalk()" )]
        public static void ToggleForceWalk()
        {
            _forceWalk = !_forceWalk;

            UOC.SetForceWalk( _forceWalk );
            UOC.SystemMessage( _forceWalk ? Strings.Force_Walk_On : Strings.Force_Walk_Off );
        }

        [CommandsDisplay( Category = "Movement", Description = "Turn in the given direction.",
            InsertText = "Turn(\"east\")" )]
        public static void Turn( string direction )
        {
            Direction directionEnum = Utility.GetEnumValueByName<Direction>( direction );

            if ( Engine.Player.Direction == directionEnum )
            {
                return;
            }

            Engine.Move( directionEnum, false );
            UOC.WaitForIncomingPacket( new PacketFilterInfo( 22 ), MOVEMENT_TIMEOUT );
        }

        [CommandsDisplay( Category = "Movement", Description = "Run in the given direction.",
            InsertText = "Run(\"east\")" )]
        public static bool Run( string direction )
        {
            return Move( direction, true );
        }

        private static bool Move( string direction, bool run )
        {
            Direction directionEnum = Utility.GetEnumValueByName<Direction>( direction );

            if ( directionEnum == Direction.Invalid )
            {
                return false;
            }

            bool result = Engine.Move( directionEnum, run );

            return result;
        }
    }
}