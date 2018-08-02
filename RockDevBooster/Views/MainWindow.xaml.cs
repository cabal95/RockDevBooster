using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using com.blueboxmoon.RockDevBooster.Properties;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Popup _lastPopup = null;

        public MainWindow()
        {
            InitializeComponent();

            foreach ( TabItem item in tcMain.Items )
            {
                item.Visibility = Visibility.Collapsed;
            }

            ActivateMenuSelection( btnMenuLauncherInstances );
        }

        /// <summary>
        /// Activates a new menu button and switches the current view.
        /// </summary>
        /// <param name="button">The button.</param>
        protected void ActivateMenuSelection( Button button )
        {
            
            if ( button.GetPopup() != null )
            {
                _lastPopup = button.GetPopup();
                _lastPopup.PlacementTarget = button;
                _lastPopup.IsOpen = true;

                return;
            }

            Button menuButton = button;
            var parameters = button.CommandParameter.ToString().Split( ',' );
            string name = null;

            if (parameters.Length == 1)
            {
                name = parameters[0];
            }
            else
            {
                menuButton = ( Button ) FindName( parameters[0] );
                name = parameters[1];
            }

            tcMain.SelectedIndex = tcMain.Items.Cast<TabItem>().ToList().FindIndex( i => i.Header.ToString() == name );

            var defaultStyle = ( Style ) FindResource( "buttonStyleMenuIcon" );
            btnMenuLauncher.Style = defaultStyle;
            btnMenuGitHub.Style = defaultStyle;
            btnMenuPlugin.Style = defaultStyle;
            btnMenuSettings.Style = defaultStyle;

            menuButton.Style = ( Style ) FindResource( "buttonStyleMenuIconActive" );

            var grid = tcMain.SelectedContent as Grid;
            if ( grid != null )
            {
                var view = grid.Children[0] as IViewDidShow;
                if ( view != null )
                {
                    view.ViewDidShow();
                }
            }

            //
            // Close any menu popups.
            //
            if ( _lastPopup != null && _lastPopup.IsOpen )
            {
                _lastPopup.IsOpen = false;
            }
        }

        #region Events

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closing" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs" /> that contains the event data.</param>
        protected override void OnClosing( CancelEventArgs e )
        {
            Settings.Default.MainWindowPlacement = WindowPlacement.GetPlacement( new WindowInteropHelper( this ).Handle );
            Settings.Default.Save();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.SourceInitialized" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnSourceInitialized( EventArgs e )
        {
            base.OnSourceInitialized( e );
            WindowPlacement.SetPlacement( new WindowInteropHelper( this ).Handle, Settings.Default.MainWindowPlacement );
        }

        /// <summary>
        /// Handles the Click event of the btnMenu control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnMenu_Click( object sender, RoutedEventArgs e )
        {
            ActivateMenuSelection( ( Button ) sender );
        }

        #endregion
    }
}
