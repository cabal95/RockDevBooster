using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

using Newtonsoft.Json;

using com.blueboxmoon.RockDevBooster.Shared.PluginFormat;
using System.ComponentModel;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for SetupPluginLinksView.xaml
    /// </summary>
    public partial class SetupPluginLinksView : UserControl
    {
        #region Constructors

        /// <summary>
        /// Create a new PackageView control.
        /// </summary>
        public SetupPluginLinksView()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Setup links for a package/plugin.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnSetupLinks_Click( object sender, RoutedEventArgs e )
        {
            //
            // Ask for the user to select the plugin file.
            //
            var openFileDialog = new OpenFileDialog
            {
                DereferenceLinks = false,
                Filter = "Plugin Definitions (*.json)|*.json|All Files (*.*)|*.*"
            };

            if ( openFileDialog.ShowDialog() == false )
            {
                return;
            }

            var pluginFile = openFileDialog.FileName;

            //
            // Try to read the plugin file.
            //
            try
            {
                var plugin = JsonConvert.DeserializeObject<Plugin>( File.ReadAllText( pluginFile ) );
                plugin.ConfigureDefaults();
            }
            catch
            {
                MessageBox.Show( "Could not read plugin file.", "Build Error", MessageBoxButton.OK );
                return;
            }


            var openDirectoryDialog = new WPFFolderBrowser.WPFFolderBrowserDialog( "Rock or RockIt folder" )
            {
                DereferenceLinks = false
            };

            if ( openDirectoryDialog.ShowDialog() == false )
            {
                return;
            }
            var folder = openDirectoryDialog.FileName;

            if ( !File.Exists( Path.Combine( folder, "KeepAlive.aspx.cs" ) ) && !Directory.Exists( Path.Combine( folder, "RockWeb" ) ) )
            {
                MessageBox.Show( "This does not appear to be a RockIt or Rock production folder.", "Bad Selection", MessageBoxButton.OK );
                return;
            }

            try
            {
                var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                path = Path.Combine( Path.GetDirectoryName( path ), "setuplinks.exe" );

                var arguments = string.Format( "\"{0}\" \"{1}\"", pluginFile, folder );
                var processInfo = new System.Diagnostics.ProcessStartInfo( path, arguments )
                {
                    Verb = "runas"
                };

                using ( var process = System.Diagnostics.Process.Start( processInfo ) )
                {
                    process.WaitForExit();

                    if ( process.ExitCode != 0 )
                    {
                        MessageBox.Show( "Unknown error encountered while trying to setup project links.", "Error", MessageBoxButton.OK );
                    }
                    else
                    {
                        MessageBox.Show( "Project links have been configured.", "Success", MessageBoxButton.OK );
                    }
                }
            }
            catch ( Exception ex )
            {
                MessageBox.Show( ex.Message, "Error", MessageBoxButton.OK );
            }
        }

        #endregion
    }
}
