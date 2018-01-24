using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

using System.Data.SqlLocalDb;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for InstancesView.xaml
    /// </summary>
    public partial class InstancesView : UserControl
    {
        #region Private Fields

        /// <summary>
        /// Identifies the currently executing IIS Express process.
        /// </summary>
        private ConsoleApp iisExpressProcess = null;

        /// <summary>
        /// Identifies the SQL Server Local DB instance that we are running.
        /// </summary>
        private SqlLocalDbInstance localDb = null;

        /// <summary>
        /// Identifies the main instances view so we can refresh the state externally.
        /// </summary>
        static private InstancesView DefaultInstancesView = null;

        /// <summary>
        /// Template to be used when building the webConnectionString.config file.
        /// </summary>
        string connectionStringTemplate = @"<?xml version=""1.0""?>
<connectionStrings>
    <add name=""RockContext"" connectionString=""Data Source=(LocalDB)\{0};AttachDbFileName=|DataDirectory|\Database.mdf; Initial Catalog={1}; Integrated Security=true; MultipleActiveResultSets=true"" providerName=""System.Data.SqlClient""/>
</connectionStrings>
";

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new instance of the control.
        /// </summary>
        public InstancesView()
        {
            InitializeComponent();

            DefaultInstancesView = this;
            UpdateInstances();

            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Update the list of instances in the window.
        /// </summary>
        static public void UpdateInstances()
        {
            DefaultInstancesView.txtStatus.Text = "Loading...";

            DefaultInstancesView.btnDelete.IsEnabled = false;
            DefaultInstancesView.btnStart.IsEnabled = false;
            DefaultInstancesView.btnStop.IsEnabled = false;

            new Task( DefaultInstancesView.LoadData ).Start();
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all github tags in the background.
        /// </summary>
        private void LoadData()
        {
            //
            // Initialize the LocalDb service only once.
            //
            if ( localDb == null )
            {
                var provider = new SqlLocalDbProvider();
                SqlLocalDbInstance instance;

                try
                {
                    //
                    // If we find an existing instance then shut it down and delete it.
                    //
                    instance = provider.GetInstance( "RockLauncher" );
                    if ( instance.IsRunning )
                    {
                        instance.Stop();
                    }
                    SqlLocalDbInstance.Delete( instance );
                }
                finally
                {
                    //
                    // Create a new instance and keep a reference to it.
                    //
                    localDb = provider.CreateInstance( "RockLauncher" );
                    localDb.Start();
                }
            }

            //
            // Load all the instances from the file system.
            //
            var instances = Directory.GetDirectories( Support.GetInstancesPath() )
                .Select( d => Path.GetFileName( d ) ).ToList();

            //
            // Convert pre-1.0 instance folders to 1.0 instance folders, which contain a
            // RockWeb for the current instance data.
            //
            foreach ( string instance in instances )
            {
                string instancePath = Path.Combine( Support.GetInstancesPath(), instance );
                string rockwebPath = Path.Combine( instancePath, "RockWeb" );

                if ( !Directory.Exists( rockwebPath ) )
                {
                    Directory.CreateDirectory( rockwebPath );

                    foreach ( var d in Directory.GetDirectories( instancePath ) )
                    {
                        if ( !Path.GetFileName( d ).Equals( "RockWeb", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            Directory.Move( d, Path.Combine( rockwebPath, Path.GetFileName( d ) ) );
                        }
                    }

                    foreach ( var f in Directory.GetFiles( instancePath ) )
                    {
                        Directory.Move( f, Path.Combine( rockwebPath, Path.GetFileName( f ) ) );
                    }
                }
            }

            //
            // Update the UI with the new list of instances.
            //
            Dispatcher.Invoke( () =>
            {
                cbInstances.ItemsSource = instances;
                if ( instances.Count > 0 )
                {
                    cbInstances.SelectedIndex = 0;
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
            bool enableButtons = cbInstances.SelectedIndex != -1;

            btnStart.IsEnabled = enableButtons && iisExpressProcess == null;
            btnStop.IsEnabled = enableButtons && iisExpressProcess != null;
            btnDelete.IsEnabled = enableButtons && iisExpressProcess == null;
        }

        /// <summary>
        /// Write the web.ConnectionStrings.config file for the given RockWeb folder.
        /// </summary>
        /// <param name="rockWeb">The folder that contains the Rock instance.</param>
        private void ConfigureConnectionString( string rockWeb )
        {
            string dbName = Path.GetFileName( rockWeb );
            string configFile = Path.Combine( rockWeb, "web.ConnectionStrings.config" );

            string contents = string.Format( connectionStringTemplate, "RockLauncher", dbName );
            if ( File.Exists( configFile ) )
            {
                File.Delete( configFile );
            }

            File.WriteAllText( configFile, contents );
        }

        /// <summary>
        /// The the path to the IIS Express executable.
        /// </summary>
        /// <returns>The path to the executable.</returns>
        private string GetIisExecutable()
        {
            var key = Environment.Is64BitOperatingSystem ? "programfiles(x86)" : "programfiles";
            var programfiles = Environment.GetEnvironmentVariable( key );

            //check file exists
            var iisPath = string.Format( "{0}\\IIS Express\\iisexpress.exe", programfiles );
            if ( !File.Exists( iisPath ) )
            {
                throw new ArgumentException( "IIS Express executable not found", iisPath );
            }

            return iisPath;
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Notification that the application is about to shutdown. Terminate all our child
        /// processes.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void Dispatcher_ShutdownStarted( object sender, EventArgs e )
        {
            //
            // If we have an IIS Express process, then kill it.
            //
            if ( iisExpressProcess != null )
            {
                iisExpressProcess.Kill();
                iisExpressProcess = null;
            }

            //
            // If we have a LocalDb instance, then kill it off.
            //
            if ( localDb != null )
            {
                if ( localDb.IsRunning )
                {
                    localDb.Stop();
                }

                localDb = null;
            }

            UpdateState();
        }

        /// <summary>
        /// The user has changed the selection of the instances list. Update the UI button states.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void cbInstances_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        /// <summary>
        /// Start up a new IIS Express instance for the Rock instance that is selected.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void btnStart_Click( object sender, RoutedEventArgs e )
        {
            txtStatus.Text = "Starting...";
            txtConsole.Text = string.Empty;

            //
            // Just in case something goes wrong and they managed to type in a non-numeric.
            //
            if ( !int.TryParse( txtPort.Text, out int port ) )
            {
                port = 6229;
            }

            //
            // Find the path to the RockWeb instance.
            //
            var items = ( List<string> ) cbInstances.ItemsSource;
            var path = Path.Combine( Support.GetInstancesPath(), items[cbInstances.SelectedIndex], "RockWeb" );

            //
            // Check if the Database file already exists and if not create the
            // Run.Migration file so Rock initializes itself.
            //
            var dbPath = Path.Combine( path, "App_Data", "Database.mdf" );
            if ( !File.Exists( dbPath ) )
            {
                var runMigrationPath = Path.Combine( path, "App_Data", "Run.Migration" );
                File.WriteAllText( runMigrationPath, string.Empty );
            }

            ConfigureConnectionString( path );

            //
            // Launch the IIS Express process for this RockWeb.
            //
            iisExpressProcess = new ConsoleApp( GetIisExecutable() );
            iisExpressProcess.ProcessExited += IisExpressProcess_Exited;
            iisExpressProcess.StandardTextReceived += IisExpressProcess_StandardTextReceived;
            iisExpressProcess.ExecuteAsync( String.Format( "/path:\"{0}\"", path ), String.Format( "/port:{0}", port ) );

            //
            // Update the status text to contain a clickable link to the instance.
            //
            var linkText = string.Format( "http://localhost:{0}/", port );
            var link = new Hyperlink( new Run( linkText ) )
            {
                NavigateUri = new Uri( linkText )
            };
            link.RequestNavigate += StatusLink_RequestNavigate;
            txtStatus.Inlines.Clear();
            txtStatus.Inlines.Add( new Run( "Running at " ) );
            txtStatus.Inlines.Add( link );

            UpdateState();
        }

        /// <summary>
        /// Stop the current IIS Express instance.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void btnStop_Click( object sender, RoutedEventArgs e )
        {
            if ( iisExpressProcess != null )
            {
                iisExpressProcess.Kill();
                iisExpressProcess = null;
            }

            txtStatus.Text = "Idle";

            UpdateState();
        }

        /// <summary>
        /// Delete the selected instance from disk.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void btnDelete_Click( object sender, RoutedEventArgs e )
        {
            var result = MessageBox.Show( "Delete this instance?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question );

            if ( result == MessageBoxResult.Yes )
            {
                var items = cbInstances.ItemsSource as List<string>;

                string file = items[cbInstances.SelectedIndex];
                string path = Path.Combine( Support.GetInstancesPath(), file );

                Directory.Delete( path, true );

                new Thread( LoadData ).Start();
            }
        }

        /// <summary>
        /// User has clicked on a hyperlink in the status text.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void StatusLink_RequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Process.Start( e.Uri.AbsoluteUri );
        }

        /// <summary>
        /// The IIS Express process has spit out some text on the stdout stream.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="text">The text received by the console application.</param>
        private void IisExpressProcess_StandardTextReceived( object sender, string text )
        {
            Dispatcher.Invoke( () =>
             {
                 txtConsole.AppendText( text );
                 txtConsole.ScrollToEnd();
             } );
        }

        /// <summary>
        /// The IIS Express process has ended, perhaps unexpectedly.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void IisExpressProcess_Exited( object sender, EventArgs e )
        {
            iisExpressProcess = null;

            Dispatcher.Invoke( () => { UpdateState(); } );
        }

        /// <summary>
        /// User is typing text into the txtPort control. Validate it as numeric.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="e">The arguments that describe the event.</param>
        private void txtPort_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            e.Handled = new Regex( "[^0-9]+" ).IsMatch( e.Text );
        }

        #endregion
    }
}
