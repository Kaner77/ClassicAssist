﻿using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using ClassicAssist.UI.ViewModels;

namespace ClassicAssist.UI.Misc
{
    public class ListViewDoubleClickBehaviour : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseDoubleClick += OnMouseDoubleClick;
        }

        private void OnMouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            ObjectInspectorViewModel i = (ObjectInspectorViewModel) ( (ListView) sender ).DataContext;

            i.SelectedItem?.OnDoubleClick?.Invoke( i.SelectedItem );
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.MouseDoubleClick -= OnMouseDoubleClick;
        }
    }
}