using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

using Octokit;

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for GitHubVersions.xaml
    /// </summary>
    public partial class GitHubVersions : UserControl
    {
        protected ReleaseBuilder ReleaseBuilder { get; set; }

        public GitHubVersions()
        {
            InitializeComponent();

            txtStatus.Text = "Loading...";
            btnImport.IsEnabled = false;

            new Thread( LoadData ).Start();
        }

        /// <summary>
        /// Load all github tags in the background.
        /// </summary>
        private void LoadData()
        {
            var client = new GitHubClient( new ProductHeaderValue( "RockLauncher" ) );
            var tags = client.Repository.GetAllTags( "SparkDevNetwork", "Rock" );
            var minimumVersion = new Version( 1, 1, 0 );

            var vsList = Support.GetVisualStudioInstances();

            tags.Wait();
            var list = tags.Result
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

        protected void UpdateState()
        {
            var tags = cbTags.ItemsSource as List<GitHubTag>;
            btnImport.IsEnabled = cbTags.SelectedIndex != -1 && cbVisualStudio.SelectedIndex != -1 && !string.IsNullOrEmpty( tags[cbTags.SelectedIndex].Name );
        }

        private void btnImport_Click( object sender, RoutedEventArgs e )
        {
            var tags = cbTags.ItemsSource as List<GitHubTag>;
            var tag = tags[cbTags.SelectedIndex];
            var vsList = cbVisualStudio.ItemsSource as List<VisualStudioInstall>;
            var vs = vsList[cbVisualStudio.SelectedIndex];

            ReleaseBuilder = new ReleaseBuilder
            {
                DevEnvExecutable = vs.GetExecutable()
            };
            ReleaseBuilder.StatusTextChanged += ReleaseBuilder_StatusTextChanged;
            ReleaseBuilder.ConsoleOutput += ReleaseBuilder_ConsoleOutput;
            ReleaseBuilder.BuildCompleted += ReleaseBuilder_BuildCompleted;

            btnImport.IsEnabled = false;
            txtConsole.Text = string.Empty;

            new Thread( () =>
            {
                ReleaseBuilder.DownloadRelease( tag.ZipballUrl, "RockBase-" + tag.Name );
            } ).Start();
        }

        private void ReleaseBuilder_ConsoleOutput( object sender, string text )
        {
            Dispatcher.Invoke( () =>
            {
                txtConsole.AppendText( text );
                txtConsole.ScrollToEnd();
            } );
        }

        private void ReleaseBuilder_BuildCompleted( object sender, EventArgs e )
        {
            Dispatcher.Invoke( () =>
            {
                UpdateState();
                TemplatesView.UpdateTemplates();
            } );
        }

        private void ReleaseBuilder_StatusTextChanged( object sender, EventArgs e )
        {
            Dispatcher.Invoke( () =>
            {
                txtStatus.Text = ReleaseBuilder.StatusText;
            } );
        }

        private void cbTags_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        #region Classes

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
