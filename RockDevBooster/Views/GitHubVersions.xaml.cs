using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Octokit;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for GitHubVersions.xaml
    /// </summary>
    public partial class GitHubVersions : UserControl, IViewDidShow
    {
        #region Protected Properties

        /// <summary>
        /// The ReleaseBuilder object that we are using to build a release.
        /// </summary>
        protected Builders.ReleaseBuilder ReleaseBuilder { get; set; }

        protected List<GitHubItem> GitHubTags { get; set; }

        protected List<GitHubItem> GitHubBranches { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new GitHubVersion control.
        /// </summary>
        public GitHubVersions()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
                txtStatus.Text = "Loading...";
                btnImport.IsEnabled = false;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI state of the control to match our internal state.
        /// </summary>
        protected void UpdateState()
        {
            if ( cbSourceType.Text == "Release Version" )
            {
                var tags = cbSource.ItemsSource as List<GitHubItem>;
                int index = cbSource.SelectedIndex;

                btnImport.IsEnabled = index != -1 && !string.IsNullOrEmpty( tags[index].Name );
            }
            else if ( cbSourceType.Text == "Branch" )
            {
                var branches = cbSource.ItemsSource as List<GitHubItem>;
                int index = cbSource.SelectedIndex;

                btnImport.IsEnabled = index != -1 && !string.IsNullOrEmpty( branches[index].Name );
            }
            else if ( cbSourceType.Text == "SHA-1 Commit" )
            {
                btnImport.IsEnabled = !string.IsNullOrWhiteSpace( txtSource.Text );
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The view has become visible on screen.
        /// </summary>
        public void ViewDidShow()
        {
            if ( GitHubTags == null )
            {
                new Task( LoadData ).Start();
            }
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
            var branches = client.Repository.Branch.GetAll( "SparkDevNetwork", "Rock" );
            var minimumVersion = new Version( 1, 1, 0 );

            try
            {
                GitHubTags = ( await tags )
                   .Select( t => new GitHubItem( t ) )
                   .Where( t => t.Version >= minimumVersion )
                   .OrderByDescending( t => t.Version )
                   .ToList();

                GitHubBranches = ( await branches )
                   .Select( b => new GitHubItem( b ) )
                   .OrderBy( b => b.Name )
                   .ToList();
            }
            catch
            {
                GitHubTags = new List<GitHubItem>();
                GitHubBranches = new List<GitHubItem>();
            }
            GitHubTags.Insert( 0, new GitHubItem() );
            GitHubBranches.Insert( 0, new GitHubItem() );

            Dispatcher.Invoke( () =>
            {
                cbSourceType.Items.Add( "Release Version" );
                cbSourceType.Items.Add( "Branch" );
                cbSourceType.Items.Add( "SHA-1 Commit" );
                cbSourceType.SelectedIndex = 0;

                txtStatus.Text = "Ready";
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
            var vs = VisualStudioInstall.GetDefaultInstall();
            GitHubItem item;

            if ( cbSourceType.SelectedItem.ToString() == "Release Version" || cbSourceType.SelectedItem.ToString() == "Branch" )
            {
                var items = cbSource.ItemsSource as List<GitHubItem>;
                item = items[cbSource.SelectedIndex];
            }
            else
            {
                item = new GitHubItem
                {
                    Name = txtSource.Text.ToLowerInvariant(),
                    ZipballUrl = string.Format( "https://github.com/SparkDevNetwork/Rock/archive/{0}.zip", txtSource.Text.ToLowerInvariant() )
                };
            }

            //
            // Check if this template already exists.
            //
            var templateName = "RockBase-" + item.Name;
            if ( File.Exists( Path.Combine( Support.GetTemplatesPath(), templateName + ".zip" ) ) )
            {
                MessageBox.Show( "A template with the name " + templateName + " already exists.", "Cannot import", MessageBoxButton.OK, MessageBoxImage.Hand );
                return;
            }

            //
            // Initialize a new release builder to process this import operation.
            //
            ReleaseBuilder = new Builders.ReleaseBuilder();
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
                ReleaseBuilder.DownloadRelease( item.ZipballUrl, vs.GetExecutable(), "RockBase-" + item.Name );
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

        /// <summary>
        /// Handles the SelectionChanged event of the cbSourceType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void cbSourceType_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( cbSourceType.SelectedItem.ToString() == "Release Version" )
            {
                lSource.Content = "Version";
                txtSource.Visibility = Visibility.Hidden;

                cbSource.ItemsSource = GitHubTags;
                cbSource.SelectedIndex = 0;
                cbSource.Visibility = Visibility.Visible;
            }
            else if ( cbSourceType.SelectedItem.ToString() == "Branch" )
            {
                lSource.Content = "Branch";
                txtSource.Visibility = Visibility.Hidden;

                cbSource.ItemsSource = GitHubBranches;
                cbSource.SelectedIndex = 0;
                cbSource.Visibility = Visibility.Visible;
            }
            else if ( cbSourceType.SelectedItem.ToString() == "SHA-1 Commit" )
            {
                lSource.Content = "Commit";
                txtSource.Visibility = Visibility.Visible;
                cbSource.Visibility = Visibility.Hidden;
            }

            UpdateState();
        }

        #endregion

        #region Classes

        /// <summary>
        /// Helper class to identify a GutHub Tag Release.
        /// </summary>
        protected class GitHubItem
        {
            public string Name { get; set; }

            public string ZipballUrl { get; set; }

            public Version Version { get; set; }

            public GitHubItem()
            {
            }

            public GitHubItem( RepositoryTag tag )
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

            public GitHubItem( Branch branch )
            {
                Name = branch.Name;
                ZipballUrl = string.Format( "https://github.com/SparkDevNetwork/Rock/archive/{0}.zip", branch.Name );
                Version = new Version();
            }

            public override string ToString()
            {
                return Name ?? string.Empty;
            }
        }

        #endregion
    }
}
