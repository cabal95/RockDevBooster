using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.ComponentModel;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for BootstrapView.xaml
    /// </summary>
    public partial class ScriptsView : UserControl
    {
        #region Private Fields

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new instance of the control.
        /// </summary>
        public ScriptsView()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
                txtStatus.Text = "Loading...";
                btnRefresh.IsEnabled = false;
                btnRun.IsEnabled = false;

                Task.Run( () => { LoadData(); } );
            }
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all scripts in the background.
        /// </summary>
        private void LoadData()
        {
            //
            // Load all the scripts from the file system.
            //
            var scripts = Directory.GetFiles( Support.GetScriptsPath() )
                .Select( f => Path.GetFileName( f ) )
                .Where( f => f.EndsWith( ".js" ) )
                .Select( f => f.Substring( 0, f.Length - 3 ) )
                .ToList();

            //
            // Update the UI with the new list of instances.
            //
            Dispatcher.Invoke( () =>
            {
                cbScripts.ItemsSource = scripts;
                if ( scripts.Count > 0 )
                {
                    cbScripts.SelectedIndex = 0;
                }

                txtStatus.Text = "Idle";

                UpdateState();
            } );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI to match our internal state.
        /// </summary>
        private void UpdateState()
        {
            btnRun.IsEnabled = cbScripts.SelectedIndex != -1;
            btnRefresh.IsEnabled = true;
        }

        /// <summary>
        /// Logs a message from the bootstrapper.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message.</param>
        private void bootstrap_LogMessage( object sender, string message )
        {
            Dispatcher.Invoke( () =>
            {
                if ( message.StartsWith( "\r" ) )
                {
                    var lines = txtConsole.Text.Split( '\n' );
                    if ( lines.Length > 0 )
                    {
                        lines[lines.Length - 1] = string.Empty;

                        txtConsole.Text = string.Join( "\n", lines ) + message.Substring( 1 );
                    }
                }
                else
                {
                    txtConsole.AppendText( message );
                }
                txtConsole.ScrollToEnd();
            } );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// The user has changed the selection of the instances list. Update the UI button states.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void cbScripts_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        /// <summary>
        /// Handles the Click event of the btnOpenFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnOpenFolder_Click( object sender, RoutedEventArgs e )
        {
            Process.Start( Support.GetScriptsPath() );
        }

        /// <summary>
        /// Handles the Click event of the btnRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnRefresh_Click( object sender, RoutedEventArgs e )
        {
            txtStatus.Text = "Loading...";
            btnRefresh.IsEnabled = false;
            btnRun.IsEnabled = false;

            Task.Run( () => { LoadData(); } );
        }

        /// <summary>
        /// Handles the Click event of the btnRun control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnRun_Click( object sender, RoutedEventArgs e )
        {
            var bootstrap = new Bootstrap.Bootstrapper();

            bootstrap.LogMessage += bootstrap_LogMessage;

            var scriptFile = Path.Combine( Support.GetScriptsPath(), cbScripts.SelectedValue.ToString() + ".js" );

            var script = File.ReadAllText( scriptFile );

            btnRun.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            txtConsole.Text = string.Empty;

            Task.Run( () =>
            {
                bootstrap.Execute( script );
            } )
            .ContinueWith( ( t ) =>
            {
                if ( t.IsFaulted )
                {
                    bootstrap_LogMessage( this, t.Exception.InnerException.Message );
                    bootstrap_LogMessage( this, t.Exception.InnerException.StackTrace );
                }

                Dispatcher.Invoke( () => { UpdateState(); } );
            } );
        }

        #endregion
    }
}
