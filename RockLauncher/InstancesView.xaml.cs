using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

using System.Data.SqlLocalDb;
using System.Text.RegularExpressions;

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for InstancesView.xaml
    /// </summary>
    public partial class InstancesView : UserControl
    {
        ConsoleAppManager iisExpressProcess = null;
        SqlLocalDbInstance localDb = null;
        static InstancesView DefaultInstancesView = null;

        string connectionStringTemplate = @"<?xml version=""1.0""?>
<connectionStrings>
    <add name=""RockContext"" connectionString=""Data Source=(LocalDB)\{0};AttachDbFileName=|DataDirectory|\Database.mdf; Initial Catalog={1}; Integrated Security=true; MultipleActiveResultSets=true"" providerName=""System.Data.SqlClient""/>
</connectionStrings>
";

        public InstancesView()
        {
            DefaultInstancesView = this;

            InitializeComponent();

            txtStatus.Text = "Loading...";
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = false;
            btnDelete.IsEnabled = false;

            new Thread( LoadData ).Start();

            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        static public void UpdateInstances()
        {
            DefaultInstancesView.btnDelete.IsEnabled = false;
            DefaultInstancesView.btnStart.IsEnabled = false;
            DefaultInstancesView.btnStop.IsEnabled = false;

            new Thread( DefaultInstancesView.LoadData ).Start();
        }

        private void Dispatcher_ShutdownStarted( object sender, EventArgs e )
        {
            if ( iisExpressProcess != null )
            {
                iisExpressProcess.Kill();
                iisExpressProcess = null;
            }

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
        /// Load all github tags in the background.
        /// </summary>
        private void LoadData()
        {
            var provider = new SqlLocalDbProvider();
            SqlLocalDbInstance instance;

            try
            {
                instance = provider.GetInstance( "RockLauncher" );
                if ( instance.IsRunning )
                {
                    instance.Stop();
                }
                SqlLocalDbInstance.Delete( instance );
            }
            finally
            {
                instance = provider.CreateInstance( "RockLauncher" );
                localDb = instance;
            }

            var instances = Directory.GetDirectories( Support.GetInstancesPath() )
                .Select( d => System.IO.Path.GetFileName( d ) ).ToList();

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

        private void UpdateState()
        {
            bool enableButtons = cbInstances.SelectedIndex != -1;

            btnStart.IsEnabled = enableButtons && iisExpressProcess == null;
            btnStop.IsEnabled = enableButtons && iisExpressProcess != null;
            btnDelete.IsEnabled = enableButtons && iisExpressProcess == null;
        }

        private void ConfigureConnectionString( string rockWeb )
        {
            string dbName = System.IO.Path.GetFileName( rockWeb );
            string configFile = System.IO.Path.Combine( rockWeb, "web.ConnectionStrings.config" );

            string contents = string.Format( connectionStringTemplate, "RockLauncher", dbName );
            if ( File.Exists( configFile ) )
            {
                File.Delete( configFile );
            }

            File.WriteAllText( configFile, contents );
        }

        private void cbInstances_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        private void btnStart_Click( object sender, RoutedEventArgs e )
        {
            txtStatus.Text = "Starting...";
            txtConsole.Text = string.Empty;

            if ( !int.TryParse( txtPort.Text, out int port ) )
            {
                port = 6229;
            }

            var items = ( List<string> ) cbInstances.ItemsSource;
            var path = System.IO.Path.Combine( Support.GetInstancesPath(), items[cbInstances.SelectedIndex] );

            var dbPath = System.IO.Path.Combine( path, "App_Data", "Database.mdf" );
            if ( !File.Exists( dbPath ) )
            {
                var runMigrationPath = System.IO.Path.Combine( path, "App_Data", "Run.Migration" );
                File.WriteAllText( runMigrationPath, string.Empty );
            }

            ConfigureConnectionString( path );

            iisExpressProcess = new ConsoleAppManager( GetIisExecutable() );
            iisExpressProcess.ProcessExited += IisExpressProcess_Exited;
            iisExpressProcess.StandartTextReceived += IisExpressProcess_StandartTextReceived;
            iisExpressProcess.ExecuteAsync( String.Format( "/path:\"{0}\"", path ), String.Format( "/port:{0}", port ) );

            var linkText = string.Format( "http://localhost:{0}/", port );
            var link = new Hyperlink( new Run( linkText ) );
            link.NavigateUri = new Uri( linkText );
            link.RequestNavigate += Link_RequestNavigate;
            txtStatus.Inlines.Clear();
            txtStatus.Inlines.Add( new Run( "Running at " ) );
            txtStatus.Inlines.Add( link );

            UpdateState();
        }

        private void Link_RequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Process.Start( e.Uri.AbsoluteUri );
        }

        private void IisExpressProcess_StandartTextReceived( object sender, string e )
        {
            Dispatcher.Invoke( () =>
             {
                 txtConsole.Text += e;
             } );
        }

        private void IisExpressProcess_Exited( object sender, EventArgs e )
        {
            iisExpressProcess = null;

            Dispatcher.Invoke( () => { UpdateState(); } );
        }

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

        private void btnDelete_Click( object sender, RoutedEventArgs e )
        {
            var result = MessageBox.Show( "Delete this instance?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question );

            if ( result == MessageBoxResult.Yes )
            {
                var items = cbInstances.ItemsSource as List<string>;

                string file = items[cbInstances.SelectedIndex];
                string path = System.IO.Path.Combine( Support.GetInstancesPath(), file );

                Directory.Delete( path, true );

                new Thread( LoadData ).Start();
            }
        }

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

        private void txtPort_PreviewTextInput( object sender, TextCompositionEventArgs e )
        {
            e.Handled = new Regex( "[^0-9]+" ).IsMatch( e.Text );
        }
    }
}
