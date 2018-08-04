using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using com.blueboxmoon.RockDevBooster.Properties;
using Octokit;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesView : UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
                var vsList = VisualStudioInstall.GetVisualStudioInstances();
                var vs = VisualStudioInstall.GetDefaultInstall();

                cbVisualStudio.ItemsSource = vsList;

                if ( vs != null )
                {
                    int selectedIndex = vsList.IndexOf( vsList.Where( v => v.Path == vs.Path ).FirstOrDefault() );
                    cbVisualStudio.SelectedIndex = selectedIndex;
                }

                cbAutoUpdate.IsChecked = Settings.Default.AutoUpdate;
            }

            CheckForUpdatesAndPromptUser();
        }

        #region Methods

        /// <summary>
        /// Checks for updates and prompt user.
        /// </summary>
        protected void CheckForUpdatesAndPromptUser( bool force = false )
        {
            if ( !force )
            {
                var lastCheck = Settings.Default.LastUpdateCheck;

                //
                // Only check once a week and if we are auto updating.
                //
                if ( lastCheck.AddDays( 7 ) >= DateTime.Now || !Settings.Default.AutoUpdate )
                {
                    return;
                }
            }

            CheckForUpdatesAsync().ContinueWith( ( t ) =>
            {
                Settings.Default.LastUpdateCheck = DateTime.Now;

                if ( !t.IsFaulted && t.Result )
                {
                    Dispatcher.Invoke( () =>
                    {
                        var response = new Dialogs.PendingUpdateDialog().ShowDialog();

                        if ( response.HasValue && response.Value )
                        {
                            Process.Start( "https://github.com/cabal95/RockDevBooster/releases" );
                        }
                    } );
                }
            } );
        }

        /// <summary>
        /// Checks for updates.
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var client = new GitHubClient( new ProductHeaderValue( "RockDevBooster" ) );
                var release = await client.Repository.Release.GetLatest( "cabal95", "RockDevBooster" );

                if ( release == null || !release.Name.StartsWith( "Version " ) )
                {
                    return false;
                }

                var version = Version.Parse( release.Name.Substring( 8 ) );
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                return version > currentVersion;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the SelectionChanged event of the cbVisualStudio control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void cbVisualStudio_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            var vsList = cbVisualStudio.ItemsSource as List<VisualStudioInstall>;
            var vs = vsList[cbVisualStudio.SelectedIndex];
            
            Settings.Default.VisualStudioVersion = vs.Path;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the Click event of the btnCheckForUpdates control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnCheckForUpdates_Click( object sender, RoutedEventArgs e )
        {
            CheckForUpdatesAndPromptUser( true );
        }

        /// <summary>
        /// Handles the Checked event of the cbAutoUpdate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void cbAutoUpdate_Changed( object sender, RoutedEventArgs e )
        {
            Settings.Default.AutoUpdate = cbAutoUpdate.IsChecked.Value;
            Settings.Default.Save();
        }

        /// <summary>
        /// Handles the Click event of the btnOpenDataFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnOpenDataFolder_Click( object sender, RoutedEventArgs e )
        {
            Process.Start( Support.GetDataPath() );
        }

        #endregion
    }
}
