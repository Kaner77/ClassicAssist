﻿using Assistant;
using ClassicAssist.Resources;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Objects;
using UOC = ClassicAssist.UO.Commands;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class MobileCommands
    {
        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns true if given mobile is dead, false if not, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "if Dead(\"self\"):" )]
        public static bool Dead( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.IsDead;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return false;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns true if given mobile is hidden, false if not, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "if Hidden(\"self\"):" )]
        public static bool Hidden( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.Status.HasFlag( MobileStatus.Hidden );
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return false;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns the given mobiles hitpoints, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "hits = Hits(\"self\")" )]
        public static int Hits( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return 0;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.Hits;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return 0;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns the given mobiles max hitpoints, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "hits = MaxHits(\"self\")" )]
        public static int MaxHits( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return 0;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.Hits;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return 0;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns the given mobiles difference between max and current hits, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "if DiffHits(\"self\") > 50:" )]
        public static int DiffHits( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return 0;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.HitsMax - mobile.Hits;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return 0;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns the given mobiles stamina, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "if Stam(\"self\") < 25:" )]
        public static int Stam( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return 0;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.Stamina;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return 0;
        }

        [CommandsDisplay( Category = "Entity",
            Description =
                "Returns the given mobiles mana, if parameter is null, then returns the value from the player (parameter can be serial or alias).",
            InsertText = "if Mana(\"self\") < 25:" )]
        public static int Mana( object obj = null )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial <= 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return 0;
            }

            Mobile mobile = Engine.Mobiles.GetMobile( serial );

            if ( mobile != null )
            {
                return mobile.Mana;
            }

            UOC.SystemMessage( Strings.Mobile_not_found___ );
            return 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Checks whether a mobile is in war mode.",
            InsertText = "if War(\"self\"):" )]
        public static bool War( object obj )
        {
            int serial = AliasCommands.ResolveSerial( obj );

            if ( serial == 0 )
            {
                UOC.SystemMessage( Strings.Invalid_or_unknown_object_id );
                return false;
            }

            return Engine.Mobiles.GetMobile( serial )?.Status.HasFlag( MobileStatus.WarMode ) ?? false;
        }

        [CommandsDisplay( Category = "Entity",
            Description = "Returns the number of current followers as per status bar data.",
            InsertText = "if Followers() < 1:" )]
        public static int Followers()
        {
            return Engine.Player?.Followers ?? 0;
        }

        [CommandsDisplay( Category = "Entity",
            Description = "Returns the number of max followers as per status bar data.",
            InsertText = "if Followers() == MaxFollowers():" )]
        public static int MaxFollowers()
        {
            return Engine.Player?.FollowersMax ?? 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the current weight as as per status bar data.",
            InsertText = "if Weight() > 300:" )]
        public static int Weight()
        {
            return Engine.Player?.Weight ?? 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the max weight as per status bar data.",
            InsertText = "if MaxWeight() < 300:" )]
        public static int MaxWeight()
        {
            return Engine.Player?.WeightMax ?? 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the difference between max weight and weight.",
            InsertText = "if DiffWeight() > 50:" )]
        public static int DiffWeight()
        {
            PlayerMobile player = Engine.Player;

            if ( player == null )
            {
                return 0;
            }

            return player.WeightMax - player.Weight;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the gold value as per status bar data.",
            InsertText = "if Gold() < 2000:" )]
        public static int Gold()
        {
            return Engine.Player?.Gold ?? 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the luck value as per status bar data.",
            InsertText = "if Luck() < 800:" )]
        public static int Luck()
        {
            return Engine.Player?.Luck ?? 0;
        }

        [CommandsDisplay( Category = "Entity", Description = "Returns the current players' tithing points.",
            InsertText = "if TithingPoints() < 1000:" )]
        public static int TithingPoints()
        {
            return Engine.Player?.TithingPoints ?? 0;
        }
    }
}