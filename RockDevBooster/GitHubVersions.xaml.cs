using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Octokit;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Interaction logic for GitHubVersions.xaml
    /// </summary>
    public partial class GitHubVersions : UserControl
    {
        #region Protected Properties

        /// <summary>
        /// The ReleaseBuilder object that we are using to build a release.
        /// </summary>
        protected ReleaseBuilder ReleaseBuilder { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new GitHubVersion control.
        /// </summary>
        public GitHubVersions()
        {
            InitializeComponent();

            txtStatus.Text = "Loading...";
            btnImport.IsEnabled = false;

            new Task( LoadData ).Start();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI state of the control to match our internal state.
        /// </summary>
        protected void UpdateState()
        {
            var tags = cbTags.ItemsSource as List<GitHubTag>;
            btnImport.IsEnabled = cbTags.SelectedIndex != -1 && cbVisualStudio.SelectedIndex != -1 && !string.IsNullOrEmpty( tags[cbTags.SelectedIndex].Name );
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all github tags in the background.
        /// </summary>
        private async void LoadData()
        {
            var client = new GitHubClient( new ProductHeaderValue( "RockDevBooster" ) );
            var tags = client.Repository.GetAllTags( "SparkDevNetwork", "Rock" );
            var minimumVersion = new Version( 1, 1, 0 );

            var vsList = Support.GetVisualStudioInstances();

            var list = ( await tags )
               .Select( t => new GitHubTag( t ) )
               .Where( t => t.Version >= minimumVersion )
               .OrderByDescending( t => t.Version )
               .ToList();
            list.Insert( 0, new GitHubTag() );

            Dispatcher.Invoke( () =>
            {
                cbTags.ItemsSource = list;
                txtStatus.Text = string.Empty;
                cbTags.SelectedIndex = 0;

                cbVisualStudio.ItemsSource = vsList;
                if ( vsList.Count > 0 )
                {
                    cbVisualStudio.SelectedIndex = 0;
                }
            } );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Import the given github version as a new template.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnImport_Click( object sender, RoutedEventArgs e )
        {
            var tags = cbTags.ItemsSource as List<GitHubTag>;
            var tag = tags[cbTags.SelectedIndex];
            var vsList = cbVisualStudio.ItemsSource as List<VisualStudioInstall>;
            var vs = vsList[cbVisualStudio.SelectedIndex];

            //
            // Check if this template already exists.
            //
            var templateName = "RockBase-" + tag.Name;
            if ( File.Exists( Path.Combine( Support.GetTemplatesPath(), templateName + ".zip" ) ) )
            {
                MessageBox.Show( "A template with the name " + templateName + " already exists.", "Cannot import", MessageBoxButton.OK, MessageBoxImage.Hand );
                return;
            }

            //
            // Initialize a new release builder to process this import operation.
            //
            ReleaseBuilder = new ReleaseBuilder();
            ReleaseBuilder.StatusTextChanged += ReleaseBuilder_StatusTextChanged;
            ReleaseBuilder.ConsoleOutput += ReleaseBuilder_ConsoleOutput;
            ReleaseBuilder.BuildCompleted += ReleaseBuilder_BuildCompleted;

            //
            // Prepare the UI for the import operation.
            //
            btnImport.IsEnabled = false;
            txtConsole.Text = string.Empty;

            //
            // Start a task in the background to download and build the github release.
            //
            new Task( () =>
            {
                ReleaseBuilder.DownloadRelease( tag.ZipballUrl, vs.GetExecutable(), "RockBase-" + tag.Name );
            } ).Start();
        }

        /// <summary>
        /// The user has changed selection in the list of github tags. Update the UI.
        /// </summary>
        /// <param name="sender">The object that sent this event.</param>
        /// <param name="e">The arguments describing this event.</param>
        private void cbTags_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        /// <summary>
        /// Text that was intended for the console output has been received. Display it.
        /// </summary>
        /// <param name="sender">The object that is sending this event.</param>
        /// <param name="text">The text from the console stream.</param>
        private void ReleaseBuilder_ConsoleOutput( object sender, string text )
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
        private void ReleaseBuilder_BuildCompleted( object sender, EventArgs e )
        {
            Dispatcher.Invoke( () =>
            {
                UpdateState();
                TemplatesView.UpdateTemplates();
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

        #region Classes

        /// <summary>
        /// Helper class to identify a GutHub Tag Release.
        /// </summary>
        protected class GitHubTag
        {
            public string Name { get; set; }

            public string ZipballUrl { get; set; }

            public Version Version { get; set; }

            public GitHubTag()
            {
            }

            public GitHubTag( RepositoryTag tag )
            {
                Name = tag.Name;
                ZipballUrl = tag.ZipballUrl;

                Version v;
                if ( !Version.TryParse( Name, out v ) )
                {
                    v = new Version();
                }
                Version = v;
            }

            public override string ToString()
            {
                return Name ?? string.Empty;
            }
        }

        #endregion
    }
}
