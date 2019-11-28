﻿using System.Collections.Generic;

namespace ClassicAssist.Data.Macros.Commands
{
    public static class ListCommands
    {
        private static readonly Dictionary<string, List<int>> _lists = new Dictionary<string, List<int>>();

        [CommandsDisplay(Category = "Lists", Description = "Create list with given name, if list already exists, it is overwritten.")]
        public static void CreateList( string listName )
        {
            if ( ListExists( listName ) )
            {
                RemoveList( listName );
            }

            _lists.Add( listName, new List<int>() );
        }

        [CommandsDisplay(Category = "Lists", Description = "Returns true if list exist, or false if not.")]
        public static bool ListExists( string listName )
        {
            return _lists.ContainsKey( listName );
        }

        [CommandsDisplay(Category = "Lists", Description = "Pushes a value to the end of the list, will create list if it doesn't exist.")]
        public static void PushList( string listName, int value )
        {
            if ( !ListExists( listName ) )
            {
                CreateList( listName );
            }

            _lists[listName].Add( value );
        }

        [CommandsDisplay(Category = "Lists", Description = "Returns array of all entries in the list, for use with for loop etc.")]
        public static int[] GetList( string listName )
        {
            return _lists[listName].ToArray();
        }

        [CommandsDisplay(Category = "Lists", Description = "Removes the list with the given name.")]
        public static void RemoveList( string listName )
        {
            _lists.Remove( listName );
        }

        public static Dictionary<string, List<int>> GetAllLists()
        {
            return _lists;
        }
    }
}