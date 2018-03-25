using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

using Newtonsoft.Json;

using com.blueboxmoon.PluginFormat;

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

        /// <summary>
        /// The plugin that was loaded.
        /// </summary>
        protected Plugin Plugin { get; set; }

        /// <summary>
        /// The path to the plugin file that was loaded.
        /// </summary>
        protected string PluginPath { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new PackageView control.
        /// </summary>
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

        /// <summary>
        /// Translate a path by replacing special character strings with references
        /// in the plugin data.
        /// </summary>
        /// <param name="plugin">The plugin to use for reference replacement.</param>
        /// <param name="path">The path string to be translated.</param>
        /// <returns>A new path with the replacements done.</returns>
        protected string TranslatePluginPath( Plugin plugin, string path )
        {
            return path
                .Replace( "{Name}", plugin.Name )
                .Replace( "{Organization}", plugin.Organization )
                .Replace( "{PluginPath}", plugin.PluginPath )
                .Replace( "{ControlsPath}", plugin.ControlsPath )
                .Replace( "{ThemesPath}", plugin.ThemesPath )
                .Replace( "{WebhooksPath}", plugin.WebhooksPath );
        }

        /// <summary>
        /// Get the full path to a relative path to the project.
        /// </summary>
        /// <param name="plugin">The plugin to use for replacement services.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path to the file.</returns>
        protected string GetPluginRelativePath( Plugin plugin, string relativePath )
        {
            string path = null;

            path = CombinePaths( PluginPath, relativePath, plugin );

            return Path.GetFullPath( path );
        }

        /// <summary>
        /// Combine two path components into a final path.
        /// </summary>
        /// <param name="mainPath">The left side of the path to be combined.</param>
        /// <param name="path">The right side of the path to be combined.</param>
        /// <param name="plugin">If not null, the path will be translated by the plugin references first.</param>
        /// <returns>The full combined path.</returns>
        protected string CombinePaths( string mainPath, string path, Plugin plugin )
        {
            List<string> paths = new List<string> { mainPath };

            if ( plugin != null )
            {
                path = TranslatePluginPath( plugin, path );
            }

            paths.AddRange( path.Split( '/', '\\' ) );

            return Path.Combine( paths.ToArray() );
        }

        /// <summary>
        /// Copy all files and directories recursively from the source path to the destination path.
        /// </summary>
        /// <param name="sourcePath">The path whose contents will be copied.</param>
        /// <param name="destinationPath">The path where the contents will be copied into.</param>
        protected void CopyFiles( string sourcePath, string destinationPath )
        {
            if ( Directory.Exists( sourcePath ) )
            {
                var files = GetFileList( sourcePath );
                string stripName = sourcePath.EndsWith( "\\" ) ? sourcePath : string.Format( "{0}\\", sourcePath );

                foreach ( var file in files )
                {
                    var strippedFile = file.Replace( stripName, string.Empty );
                    var dest = CombinePaths( destinationPath, strippedFile, null );

                    if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
                    {
                        Directory.CreateDirectory( Path.GetDirectoryName( dest ) );
                    }

                    CopyFile( file, dest );
                }
            }
        }

        /// <summary>
        /// Copy a file and log the message.
        /// </summary>
        /// <param name="sourcePath">The source file to copy.</param>
        /// <param name="destinationPath">The destination file path to copy to.</param>
        protected void CopyFile( string sourcePath, string destinationPath )
        {
            txtConsole.AppendText( string.Format( "Copying \"{0}\" to \"{1}\"\n", sourcePath, destinationPath ) );
            txtConsole.ScrollToEnd();

            File.Copy( sourcePath, destinationPath );
        }

        /// <summary>
        /// Copies all the DLLs from the bin\Release directory of the project into
        /// the destination path.
        /// </summary>
        /// <param name="dlls">A list of DLL names to be copied.</param>
        /// <param name="destinationPath">The path to copy all the files to.</param>
        protected void CopyDLLs( string projectPath, List<string> dlls, string destinationPath )
        {
            foreach ( var file in dlls )
            {
                string dest = Path.Combine( destinationPath, file );

                if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
                {
                    Directory.CreateDirectory( Path.GetDirectoryName( dest ) );
                }

                CopyFile( Path.Combine( projectPath, "bin", "Release", file ), Path.Combine( destinationPath, file ) );
            }
        }

        /// <summary>
        /// Gets a list of absolute paths to all files contained in the directory, recursively.
        /// </summary>
        /// <param name="path">The path to be searched.</param>
        /// <returns>A list of strings representing the file names and paths.</returns>
        protected List<string> GetFileList( string path )
        {
            var files = new List<string>();

            foreach ( string f in Directory.GetFiles( path ) )
            {
                files.Add( f );
            }

            foreach ( string d in Directory.GetDirectories( path ) )
            {
                files.AddRange( GetFileList( d ) );
            }

            return files;
        }

        /// <summary>
        /// Build a package from all the components available.
        /// </summary>
        protected void BuildPackage()
        {
            txtStatus.Text = "Packaging...";

            //
            // Get all our staging paths.
            //
            var stagingPath = Support.GetPackageBuildPath();
            var contentPath = Path.Combine( stagingPath, "content" );
            var installPath = Path.Combine( stagingPath, "install" );
            var uninstallPath = Path.Combine( stagingPath, "uninstall" );

            //
            // Get the filename the user wants to save the plugin into.
            //
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.DereferenceLinks = false;
            saveFileDialog.FileName = Plugin.Name.Replace( " ", "" ) + ".plugin";
            saveFileDialog.DefaultExt = "plugin";
            saveFileDialog.Filter = "Plugin Files (*.plugin)|*.plugin|All Files (*.*)|*.*";
            if ( saveFileDialog.ShowDialog() == false )
            {
                txtStatus.Text = "Cancelled";
                UpdateState();

                return;
            }
            string pluginFileName = saveFileDialog.FileName;

            txtConsole.AppendText( string.Format( "\nBuilding package \"{0}\"\n", pluginFileName ) );
            txtConsole.ScrollToEnd();

            //
            // Delete any failed builds.
            //
            if ( Directory.Exists( stagingPath ) )
            {
                Directory.Delete( stagingPath, true );
            }

            //
            // Create all our staging paths.
            //
            Directory.CreateDirectory( stagingPath );
            Directory.CreateDirectory( contentPath );
            Directory.CreateDirectory( installPath );
            Directory.CreateDirectory( uninstallPath );

            //
            // Copy the Controls, Themes, Webhooks.
            //
            CopyFiles( GetPluginRelativePath( Plugin, Plugin.ControlsPath ), CombinePaths( contentPath, Plugin.PluginPath, Plugin ) );
            CopyFiles( GetPluginRelativePath( Plugin, Plugin.ThemesPath ), CombinePaths( contentPath, "Themes", null ) );
            CopyFiles( GetPluginRelativePath( Plugin, Plugin.WebhooksPath ), CombinePaths( contentPath, "Webhooks", null ) );

            //
            // Copy DLLs.
            //
            string projectFile = GetPluginRelativePath( Plugin, Plugin.ProjectFile );
            if ( File.Exists( projectFile ) )
            {
                var dlls = GetFileList( Path.Combine( PluginPath, "obj", "Release" ) )
                    .Select( f => Path.GetFileName( f ) )
                    .Where( f => f.EndsWith( ".dll" ) )
                    .ToList();
                dlls.AddRange( Plugin.DLLs );
                CopyDLLs( Path.GetDirectoryName( GetPluginRelativePath( Plugin, Plugin.ProjectFile ) ), dlls, Path.Combine( contentPath, "bin" ) );
            }

            //
            // Copy the SQL scripts.
            //
            if ( !string.IsNullOrWhiteSpace( Plugin.InstallSql ) )
            {
                CopyFile( GetPluginRelativePath( Plugin, Plugin.InstallSql ), Path.Combine( installPath, "run.sql" ) );
            }
            if ( !string.IsNullOrWhiteSpace( Plugin.UninstallSql ) )
            {
                CopyFile( GetPluginRelativePath( Plugin, Plugin.UninstallSql ), Path.Combine( uninstallPath, "run.sql" ) );
            }

            //
            // Copy any additional files.
            //
            foreach ( var file in Plugin.Copy )
            {
                CopyFile( GetPluginRelativePath( Plugin, file.Source ), CombinePaths( contentPath, file.Destination, Plugin ) );
            }

            //
            // Build the install deletefile.lst.
            //
            var files = Plugin.RemoveFilesOnInstall.Select( f => f.Replace( "/", "\\" ) ).ToList();
            File.WriteAllLines( Path.Combine( installPath, "deletefile.lst" ), files.ToArray() );

            //
            // Build the uninstall deletefile.lst.
            //
            string stripPath = string.Format( "{0}\\", contentPath );
            files = GetFileList( contentPath ).Select( f => f.Replace( stripPath, string.Empty ) ).ToList();
            files.AddRange( Plugin.RemoveFilesOnUninstall.Select( f => f.Replace( "/", "\\" ) ) );
            File.WriteAllLines( Path.Combine( uninstallPath, "deletefile.lst" ), files.ToArray() );

            //
            // Zip it all up.
            //
            txtConsole.AppendText( "Compressing zip file\n" );
            txtConsole.ScrollToEnd();
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

            var openFileDialog = new OpenFileDialog();
            openFileDialog.DereferenceLinks = false;
            openFileDialog.Filter = "Plugin Definitions (*.json)|*.json|All Files (*.*)|*.*";
            if ( openFileDialog.ShowDialog() == false )
            {
                return;
            }
            PluginPath = Path.GetDirectoryName( openFileDialog.FileName );

            //
            // Try to read the plugin file.
            //
            try
            {
                var pluginJson = File.ReadAllText( Path.Combine( PluginPath, "rockplugin.json" ) );
                Plugin = JsonConvert.DeserializeObject<Plugin>( pluginJson );
                Plugin.ConfigureDefaults();
            }
            catch
            {
                MessageBox.Show( "Could not read plugin file.", "Build Error", MessageBoxButton.OK );
                return;
            }

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
            string projectFile = GetPluginRelativePath( Plugin, Plugin.ProjectFile );
            if ( File.Exists( projectFile ) )
            {
                new Task( () =>
                {
                    ProjectBuilder.BuildProject( GetPluginRelativePath( Plugin, Plugin.ProjectFile ), vs.GetMsBuild() );
                } ).Start();
            }
            else
            {
                BuildPackage();
            }
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
        private void ProjectBuilder_BuildCompleted( object sender, StatusEventArgs e )
        {
            Dispatcher.Invoke( () =>
            {
                if ( e.Status == Status.Failed )
                {
                    txtStatus.Text = "Build Failed.";
                    MessageBox.Show( e.Message, "Build Failed", MessageBoxButton.OK );
                    return;
                }

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

                    MessageBox.Show( ex.Message, "Packaging Failed", MessageBoxButton.OK );
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
