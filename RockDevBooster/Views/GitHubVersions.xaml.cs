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

        protected List<GitHubItem> GitHubPullRequests { get; set; }

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

                btnImport.IsEnabled = index != -1 && !string.IsNullOrEmpty( tags[index].DisplayName );
            }
            else if ( cbSourceType.Text == "Branch" )
            {
                var branches = cbSource.ItemsSource as List<GitHubItem>;
                int index = cbSource.SelectedIndex;

                btnImport.IsEnabled = index != -1 && !string.IsNullOrEmpty( branches[index].DisplayName );
            }
            else if ( cbSourceType.Text == "SHA-1 Commit" )
            {
                btnImport.IsEnabled = !string.IsNullOrWhiteSpace( txtSource.Text );
            }
            else if ( cbSourceType.Text == "Pull Request" )
            {
                var pullRequests = cbSource.ItemsSource as List<GitHubItem>;
                int index = cbSource.SelectedIndex;

                btnImport.IsEnabled = index != -1 && !string.IsNullOrEmpty( pullRequests[index].DisplayName );
            }
            else
            {
                btnImport.IsEnabled = false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The view has become visible on screen.
        /// </summary>
        public void ViewDidShow()
        {
            if ( cbSourceType.Items.Count == 0 )
            {
                cbSourceType.Items.Add( "" );
                cbSourceType.Items.Add( "Release Version" );
                cbSourceType.Items.Add( "Branch" );
                cbSourceType.Items.Add( "Pull Request" );
                cbSourceType.Items.Add( "SHA-1 Commit" );
                cbSourceType.SelectedIndex = 0;

                txtStatus.Text = "Ready";
            }
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all github tags in the background.
        /// </summary>
        private void LoadData()
        {
            Dispatcher.Invoke( () =>
            {
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

            if ( cbSourceType.SelectedItem.ToString() == "Release Version" || cbSourceType.SelectedItem.ToString() == "Branch" || cbSourceType.SelectedItem.ToString() == "Pull Request" )
            {
                var items = cbSource.ItemsSource as List<GitHubItem>;
                item = items[cbSource.SelectedIndex];
            }
            else
            {
                item = new GitHubItem
                {
                    DisplayName = txtSource.Text.ToLowerInvariant(),
                    ZipballUrl = string.Format( "https://github.com/SparkDevNetwork/Rock/archive/{0}.zip", txtSource.Text.ToLowerInvariant() )
                };
            }

            //
            // Check if this template already exists.
            //
            var templateName = "RockBase-" + item.Title;
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
                ReleaseBuilder.DownloadRelease( item.ZipballUrl, vs.GetExecutable(), "RockBase-" + item.Title );
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
        private async void cbSourceType_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            async Task<List<T>> LoadDataAsync<T>(Func<Task<List<T>>> factory)
            {
                List<T> data;

                cbSourceType.IsEnabled = false;
                cbSource.IsEnabled = false;
                txtSource.IsEnabled = false;
                txtStatus.Text = "Loading...";

                try
                {
                    data = await factory();
                }
                catch
                {
                    data = null;
                }

                txtSource.IsEnabled = true;
                cbSource.IsEnabled = true;
                cbSourceType.IsEnabled = true;
                txtStatus.Text = "Ready";

                return data;
            }

            if ( cbSourceType.SelectedItem.ToString() == "Release Version" )
            {
                lSource.Content = "Version";
                txtSource.Visibility = Visibility.Hidden;

                if ( GitHubTags == null )
                {
                    GitHubTags = await LoadDataAsync( async () =>
                    {
                        var client = new GitHubClient( new ProductHeaderValue( "RockDevBooster" ) );
                        client.SetRequestTimeout( TimeSpan.FromSeconds( 5 ) );
                        var minimumVersion = new Version( 1, 1, 0 );

                        var tags = ( await client.Repository.GetAllTags( "SparkDevNetwork", "Rock" ) )
                           .Select( t => new GitHubItem( t ) )
                           .Where( t => t.Version >= minimumVersion )
                           .OrderByDescending( t => t.Version )
                           .ToList();

                        tags.Insert( 0, new GitHubItem() );

                        return tags;
                    } );
                }

                cbSource.ItemsSource = GitHubTags;
                cbSource.SelectedIndex = 0;
                cbSource.Visibility = Visibility.Visible;
            }
            else if ( cbSourceType.SelectedItem.ToString() == "Branch" )
            {
                lSource.Content = "Branch";
                txtSource.Visibility = Visibility.Hidden;

                if ( GitHubBranches == null )
                {
                    GitHubBranches = await LoadDataAsync( async () =>
                    {
                        var client = new GitHubClient( new ProductHeaderValue( "RockDevBooster" ) );
                        client.SetRequestTimeout( TimeSpan.FromSeconds( 5 ) );

                        var branches = ( await client.Repository.Branch.GetAll( "SparkDevNetwork", "Rock" ) )
                           .Select( b => new GitHubItem( b ) )
                           .OrderBy( b => b.DisplayName )
                           .ToList();

                        branches.Insert( 0, new GitHubItem() );

                        return branches;
                    } );
                }

                cbSource.ItemsSource = GitHubBranches;
                cbSource.SelectedIndex = 0;
                cbSource.Visibility = Visibility.Visible;
            }
            else if ( cbSourceType.SelectedItem.ToString() == "Pull Request" )
            {
                lSource.Content = "Pull Request";
                txtSource.Visibility = Visibility.Hidden;

                if ( GitHubPullRequests == null )
                {
                    GitHubPullRequests = await LoadDataAsync( async () =>
                    {
                        var client = new GitHubClient( new ProductHeaderValue( "RockDevBooster" ) );
                        client.SetRequestTimeout( TimeSpan.FromSeconds( 5 ) );

                        var pullRequests = ( await client.Repository.PullRequest.GetAllForRepository( "SparkDevNetwork", "Rock" ) )
                            .Select( p => new GitHubItem( p ) )
                            .OrderBy( p => p.DisplayName )
                            .ToList();

                        pullRequests.Insert( 0, new GitHubItem() );

                        return pullRequests;
                    } );
                }

                cbSource.ItemsSource = GitHubPullRequests;
                cbSource.SelectedIndex = 0;
                cbSource.Visibility = Visibility.Visible;
            }
            else if ( cbSourceType.SelectedItem.ToString() == "SHA-1 Commit" )
            {
                lSource.Content = "Commit";
                txtSource.Visibility = Visibility.Visible;
                cbSource.Visibility = Visibility.Hidden;
            }
            else
            {
                lSource.Content = string.Empty;
                txtSource.Visibility = Visibility.Hidden;
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
            public string Title { get; set; }

            public string DisplayName { get; set; }

            public string ZipballUrl { get; set; }

            public Version Version { get; set; }

            public GitHubItem()
            {
            }

            public GitHubItem( RepositoryTag tag )
            {
                DisplayName = tag.Name;
                Title = tag.Name;
                ZipballUrl = tag.ZipballUrl;

                Version v;
                if ( !Version.TryParse( DisplayName, out v ) )
                {
                    v = new Version();
                }
                Version = v;
            }

            public GitHubItem( Branch branch )
            {
                DisplayName = branch.Name;
                Title = branch.Name;
                ZipballUrl = string.Format( "https://github.com/SparkDevNetwork/Rock/archive/{0}.zip", branch.Name );
                Version = new Version();
            }

            public GitHubItem( PullRequest pullRequest )
            {
                DisplayName = $"#{pullRequest.Number} {pullRequest.Title}";
                Title = $"pr-{pullRequest.Number}";
                ZipballUrl = $"https://github.com/{pullRequest.Head.Repository.FullName}/archive/{pullRequest.Head.Ref}.zip";
                Version = new Version();
            }

            public override string ToString()
            {
                return DisplayName ?? string.Empty;
            }
        }

        #endregion
    }
}
