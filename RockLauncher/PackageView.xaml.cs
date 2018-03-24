using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using com.blueboxmoon.RockLauncher.Properties;
using Microsoft.Win32;

using Newtonsoft.Json;

namespace com.blueboxmoon.PluginFormat
{
    public class Plugin
    {
        public string Name { get; set; }

        public string Organization { get; set; }

        public string PluginPath { get; set; }

        public string ControlsPath { get; set; }

        public string ThemesPath { get; set; }

        public string WebhooksPath { get; set; }

        public string InstallSql { get; set; }

        public string UninstallSql { get; set; }

        public List<string> DLLs { get; set; }

        public List<CopyFile> Copy { get; set; }

        public List<string> RemoveFilesOnInstall { get; set; }

        public List<string> RemoveFilesOnUninstall { get; set; }


        public void ConfigureDefaults()
        {
            if ( string.IsNullOrWhiteSpace( PluginPath ) )
            {
                PluginPath = string.Format( "Plugins/com_{0}/{1}", Organization.ToLower().Replace( " ", "" ), Name.Replace( " ", "" ) );
            }

            ControlsPath = ControlsPath ?? "Controls";
            ThemesPath = ThemesPath ?? "Themes";
            WebhooksPath = WebhooksPath ?? "Webhooks";
            DLLs = DLLs ?? new List<string>();
            Copy = Copy ?? new List<CopyFile>();
            RemoveFilesOnInstall = RemoveFilesOnInstall ?? new List<string>();
            RemoveFilesOnUninstall = RemoveFilesOnUninstall ?? new List<string>();
        }
    }

    public class CopyFile
    {
        public string Source { get; set; }

        public string Destination { get; set; }
    }
}

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for PackageView.xaml
    /// </summary>
    public partial class PackageView : UserControl
    {
        #region Protected Properties

        /// <summary>
        /// The ProjectBuilder object that we are using to build a package.
        /// </summary>
        protected ProjectBuilder ProjectBuilder { get; set; }

        protected string ProjectPath { get; set; }

        #endregion

        #region Constructors

        public PackageView()
        {
            InitializeComponent();

            LoadData();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI state of the control to match our internal state.
        /// </summary>
        protected void UpdateState()
        {
            btnBuildPackage.IsEnabled = cbVisualStudio.SelectedIndex != -1;
        }

        protected string CombinePaths( string mainPath, string path )
        {
            List<string> paths = new List<string> { mainPath };

            if ( path.Contains( "/" ) )
            {
                paths.AddRange( path.Split( '/' ) );
            }
            else
            {
                paths.AddRange( path.Split( '\\' ) );
            }

            return Path.Combine( paths.ToArray() );
        }

        protected void CopyFiles( string sourcePath, string destinationPath )
        {
            if ( Directory.Exists( sourcePath ) )
            {
                var files = GetFileList( sourcePath );
                string stripName = sourcePath.EndsWith( "\\" ) ? sourcePath : string.Format( "{0}\\", sourcePath );

                foreach ( var file in files )
                {
                    var strippedFile = file.Replace( stripName, string.Empty );
                    var dest = CombinePaths( destinationPath, strippedFile );

                    if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
                    {
                        Directory.CreateDirectory( Path.GetDirectoryName( dest ) );
                    }

                    File.Copy( file, dest );
                }
            }
        }

        protected void CopyDLLs( List<string> dlls, string destinationPath )
        {
            foreach ( var file in dlls )
            {
                string dest = Path.Combine( destinationPath, file );

                if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
                {
                    Directory.CreateDirectory( Path.GetDirectoryName( dest ) );
                }

                File.Copy( Path.Combine( ProjectPath, "bin", "Release", file ), Path.Combine( destinationPath, file ) );
            }
        }

        protected List<string> GetFileList( string sourcePath )
        {
            var files = new List<string>();

            foreach ( string f in Directory.GetFiles( sourcePath ) )
            {
                files.Add( f );
            }

            foreach ( string d in Directory.GetDirectories( sourcePath ) )
            {
                files.AddRange( GetFileList( d ) );
            }

            return files;
        }

        protected void BuildPackage()
        {
            txtStatus.Text = "Packaging...";

            var pluginJson = File.ReadAllText( Path.Combine( ProjectPath, "rockplugin.json" ) );
            var pluginInfo = JsonConvert.DeserializeObject<PluginFormat.Plugin>( pluginJson );
            var stagingPath = Path.Combine( Path.Combine( ProjectPath, "obj" ), "_Package" );
            var contentPath = Path.Combine( stagingPath, "content" );
            var installPath = Path.Combine( stagingPath, "install" );
            var uninstallPath = Path.Combine( stagingPath, "uninstall" );

            pluginInfo.ConfigureDefaults();

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.DereferenceLinks = false;
            saveFileDialog.FileName = pluginInfo.Name.Replace( " ", "" ) + ".plugin";
            saveFileDialog.DefaultExt = "plugin";
            saveFileDialog.Filter = "Plugin Files (*.plugin)|*.plugin";
            if ( saveFileDialog.ShowDialog() == false )
            {
                txtStatus.Text = "Cancelled";
                UpdateState();

                return;
            }
            string pluginFileName = saveFileDialog.FileName;

            if ( Directory.Exists( stagingPath ) )
            {
                Directory.Delete( stagingPath, true );
            }

            Directory.CreateDirectory( stagingPath );
            Directory.CreateDirectory( contentPath );
            Directory.CreateDirectory( installPath );
            Directory.CreateDirectory( uninstallPath );

            //
            // Copy the Controls, Themes, Webhooks.
            //
            CopyFiles( CombinePaths( ProjectPath, pluginInfo.ControlsPath ), CombinePaths( contentPath, pluginInfo.PluginPath ) );
            CopyFiles( CombinePaths( ProjectPath, pluginInfo.ThemesPath ), CombinePaths( contentPath, "Themes" ) );
            CopyFiles( CombinePaths( ProjectPath, pluginInfo.WebhooksPath ), CombinePaths( contentPath, "Webhooks" ) );

            //
            // Copy DLLs.
            //
            var dlls = GetFileList( Path.Combine( ProjectPath, "obj", "Release" ) )
                .Select( f => Path.GetFileName( f ) )
                .Where( f => f.EndsWith( ".dll" ) )
                .ToList();
            dlls.AddRange( pluginInfo.DLLs );
            CopyDLLs( dlls, Path.Combine( contentPath, "bin" ) );

            //
            // Copy the SQL scripts.
            //
            if ( !string.IsNullOrWhiteSpace( pluginInfo.InstallSql ) )
            {
                File.Copy( CombinePaths( ProjectPath, pluginInfo.InstallSql ), Path.Combine( installPath, "run.sql" ) );
            }
            if ( !string.IsNullOrWhiteSpace( pluginInfo.UninstallSql ) )
            {
                File.Copy( CombinePaths( ProjectPath, pluginInfo.UninstallSql ), Path.Combine( uninstallPath, "run.sql" ) );
            }

            //
            // Copy any additional files.
            //
            foreach ( var file in pluginInfo.Copy )
            {
                File.Copy( CombinePaths( ProjectPath, file.Source ), CombinePaths( contentPath, file.Destination ) );
            }

            //
            // Build the install deletefile.lst.
            //
            var files = pluginInfo.RemoveFilesOnInstall.Select( f => f.Replace( "/", "\\" ) ).ToList();
            File.WriteAllLines( Path.Combine( installPath, "deletefile.lst" ), files.ToArray() );

            //
            // Build the uninstall deletefile.lst.
            //
            string stripPath = string.Format( "{0}\\", contentPath );
            files = GetFileList( contentPath ).Select( f => f.Replace( stripPath, string.Empty ) ).ToList();
            files.AddRange( pluginInfo.RemoveFilesOnUninstall.Select( f => f.Replace( "/", "\\" ) ) );
            File.WriteAllLines( Path.Combine( uninstallPath, "deletefile.lst" ), files.ToArray() );

            //
            // Zip it all up.
            //
            Support.CreateZipFromFolder( pluginFileName, stagingPath );

            txtStatus.Text = "Package Built.";
            UpdateState();
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all github tags in the background.
        /// </summary>
        private void LoadData()
        {
            var vsList = Support.GetVisualStudioInstances();

            cbVisualStudio.ItemsSource = vsList;
            if ( vsList.Count > 0 )
            {
                cbVisualStudio.SelectedIndex = 0;
            }

            UpdateState();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Build a package.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnBuildPackage_Click( object sender, RoutedEventArgs e )
        {
            var vsList = cbVisualStudio.ItemsSource as List<VisualStudioInstall>;
            var vs = vsList[cbVisualStudio.SelectedIndex];

            var openFolderDialog = new WPFFolderBrowser.WPFFolderBrowserDialog();
            openFolderDialog.DereferenceLinks = false;
            if ( openFolderDialog.ShowDialog() == false )
            {
                return;
            }
            ProjectPath = openFolderDialog.FileName;

            //
            // Initialize a new release builder to process this import operation.
            //
            ProjectBuilder = new ProjectBuilder();
            ProjectBuilder.StatusTextChanged += ReleaseBuilder_StatusTextChanged;
            ProjectBuilder.ConsoleOutput += ProjectBuilder_ConsoleOutput;
            ProjectBuilder.BuildCompleted += ProjectBuilder_BuildCompleted;

            //
            // Prepare the UI for the import operation.
            //
            btnBuildPackage.IsEnabled = false;
            txtConsole.Text = string.Empty;

            //
            // Start a task in the background to download and build the github release.
            //
            new Task( () =>
            {
                ProjectBuilder.BuildProject( ProjectPath, vs.GetMsBuild() );
            } ).Start();
        }

        /// <summary>
        /// Text that was intended for the console output has been received. Display it.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="text">The text from the console stream.</param>
        private void ProjectBuilder_ConsoleOutput( object sender, string text )
        {
            Dispatcher.Invoke( () =>
            {
                txtConsole.AppendText( text );
                txtConsole.ScrollToEnd();
            } );
        }

        /// <summary>
        /// A build has been completed. Update the UI and refresh the list of templates.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The arguments describing this event.</param>
        private void ProjectBuilder_BuildCompleted( object sender, EventArgs e )
        {
            Dispatcher.Invoke( () =>
            {
                txtStatus.Text = "Build Completed.";

                try
                {
                    BuildPackage();
                }
                catch ( Exception ex )
                {
                    txtConsole.AppendText( ex.Message );
                    txtConsole.ScrollToEnd();

                    txtStatus.Text = "Packaging Failed";
                    UpdateState();
                }
            } );
        }

        /// <summary>
        /// The status of the ReleaseBuilder has changed.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="text">The text that should be displayed on the status line.</param>
        private void ReleaseBuilder_StatusTextChanged( object sender, string text )
        {
            Dispatcher.Invoke( () =>
            {
                txtStatus.Text = text;
            } );
        }

        #endregion
    }
}
