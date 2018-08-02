using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace com.blueboxmoon.RockDevBooster
{
    public static class Extensions
    {
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.RegisterAttached( "Popup", typeof( Popup ), typeof( Button ), new PropertyMetadata( default( Popup ) ) );

        public static void SetPopup( this Button element, Popup value )
        {
            element.SetValue( PopupProperty, value );
        }

        public static Popup GetPopup( this Button element )
        {
            return ( Popup ) element.GetValue( PopupProperty );
        }
    }
}
