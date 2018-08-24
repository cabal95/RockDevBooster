using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using com.blueboxmoon.RockDevBooster.Shared.PluginFormat;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for BuildPluginView.xaml
    /// </summary>
    public partial class BuildPluginView : UserControl
    {
        #region Protected Properties

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
        public BuildPluginView()
        {
            InitializeComponent();

            UpdateState();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI state of the control to match our internal state.
        /// </summary>
        protected void UpdateState()
        {
            btnSelect.IsEnabled = true;

            if ( Plugin != null )
            {
                lSelectedPlugin.Content = string.Format( "{0} by {1}", Plugin.Name, Plugin.Organization );
                btnBuildPlugin.IsEnabled = true;
            }
            else
            {
                lSelectedPlugin.Content = string.Empty;
                btnBuildPlugin.IsEnabled = false;
            }
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Builds the plugin asynchronous.
        /// </summary>
        /// <param name="msBuild">The ms build.</param>
        /// <returns></returns>
        protected Task BuildPluginAsync( string msBuild )
        {
            var task = new Task( () =>
            {
                string projectFile = Plugin.CombinePaths( PluginPath, Plugin.ProjectFile );
                if ( File.Exists( projectFile ) )
                {
                    //
                    // Initialize a new release builder to process this import operation.
                    //
                    var projectBuilder = new Builders.ProjectBuilder();
                    projectBuilder.StatusTextChanged += ReleaseBuilder_StatusTextChanged;
                    projectBuilder.ConsoleOutput += ProjectBuilder_ConsoleOutput;

                    if ( !projectBuilder.BuildProject( Plugin.CombinePaths( PluginPath, Plugin.ProjectFile ), msBuild ) )
                    {
                        throw new Exception( "See build log for details" );
                    }
                }

                Dispatcher.Invoke( () => { txtStatus.Text = "Packaging..."; } );

                var pluginBuilder = new Builders.PluginBuilder( Plugin, PluginPath );
                pluginBuilder.LogMessage += ProjectBuilder_ConsoleOutput;
                var stream = pluginBuilder.Build();

                Dispatcher.Invoke( () =>
                {
                    //
                    // Get the filename the user wants to save the plugin into.
                    //
                    var saveFileDialog = new SaveFileDialog
                    {
                        DereferenceLinks = false,
                        FileName = Plugin.Name.Replace( " ", "" ) + ".plugin",
                        DefaultExt = "plugin",
                        Filter = "Plugin Files (*.plugin)|*.plugin|All Files (*.*)|*.*"
                    };

                    if ( saveFileDialog.ShowDialog() == false )
                    {
                        Dispatcher.Invoke( () =>
                        {
                            txtStatus.Text = "Cancelled";
                            UpdateState();
                        } );

                        return;
                    }

                    using ( var writer = File.Open( saveFileDialog.FileName, FileMode.Create ) )
                    {
                        stream.CopyTo( writer );
                    }

                    txtStatus.Text = "Package Built";
                    UpdateState();
                } );
            } );

            task.ContinueWith( ( t ) =>
            {
                if ( t.IsFaulted )
                {
                    Dispatcher.Invoke( () =>
                    {
                        txtStatus.Text = "Build Failed";
                        MessageBox.Show( t.Exception.InnerException.Message, "Build Failed", MessageBoxButton.OK );
                        UpdateState();
                    } );
                }
            } );

            task.Start();

            return task;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Build a plugin.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnBuildPlugin_Click( object sender, RoutedEventArgs e )
        {
            var vs = VisualStudioInstall.GetDefaultInstall();

            //
            // Prepare the UI for the import operation.
            //
            btnSelect.IsEnabled = false;
            btnBuildPlugin.IsEnabled = false;
            txtConsole.Text = string.Empty;

            BuildPluginAsync( vs.GetMsBuild() );
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

        /// <summary>
        /// Handles the Click event of the btnSelect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void btnSelect_Click( object sender, RoutedEventArgs e )
        {
            var openFileDialog = new OpenFileDialog
            {
                DereferenceLinks = false,
                Filter = "Plugin Definitions (*.json)|*.json|All Files (*.*)|*.*"
            };
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
                var pluginJson = File.ReadAllText( openFileDialog.FileName );
                Plugin = JsonConvert.DeserializeObject<Plugin>( pluginJson );
                Plugin.ConfigureDefaults();
            }
            catch
            {
                Plugin = null;
                UpdateState();

                MessageBox.Show( "Could not read plugin file.", "Build Error", MessageBoxButton.OK );

                return;
            }

            UpdateState();
        }

        #endregion
    }
}
