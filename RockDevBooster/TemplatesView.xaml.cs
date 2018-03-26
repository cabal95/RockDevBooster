using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Interaction logic for TemplatesView.xaml
    /// </summary>
    public partial class TemplatesView : UserControl
    {
        #region Private Fields

        /// <summary>
        /// The singleton instance of this control.
        /// </summary>
        static private TemplatesView DefaultTemplatesView;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new instance of this control.
        /// </summary>
        public TemplatesView()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
                txtStatus.Text = string.Empty;

                UpdateState();

                DefaultTemplatesView = this;
                UpdateTemplates();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the list of templates.
        /// </summary>
        static public void UpdateTemplates()
        {
            DefaultTemplatesView.btnDeploy.IsEnabled = false;
            DefaultTemplatesView.btnDelete.IsEnabled = false;

            new Thread( DefaultTemplatesView.LoadData ).Start();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Update the UI to reflect the internal state.
        /// </summary>
        protected void UpdateState()
        {
            bool buttonsEnabled = cbTemplates.SelectedIndex != -1;

            btnDelete.IsEnabled = buttonsEnabled;
            btnDeploy.IsEnabled = buttonsEnabled;
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Load all data in the background.
        /// </summary>
        private void LoadData()
        {
            var templates = Directory.GetFiles( Support.GetTemplatesPath(), "*.zip" )
                .Select( d => System.IO.Path.GetFileName( d ) )
                .Select( f => f.Substring( 0, f.Length - 4 ) )
                .ToList();

            Dispatcher.Invoke( () =>
            {
                cbTemplates.ItemsSource = templates;
                if ( templates.Count > 0 )
                {
                    cbTemplates.SelectedIndex = 0;
                }

                UpdateState();
            } );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// The template selection has changed, update the UI state.
        /// </summary>
        /// <param name="sender">The object that has sent this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        private void cbTemplates_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        /// <summary>
        /// Deploy a template as a new instance to be run.
        /// </summary>
        /// <param name="sender">The object that has sent this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        private void btnDeploy_Click( object sender, RoutedEventArgs e )
        {
            //
            // Check if this instance name is valid.
            //
            string targetPath = System.IO.Path.Combine( Support.GetInstancesPath(), txtName.Text );
            bool isValid = !string.IsNullOrWhiteSpace( txtName.Text ) && !Directory.Exists( targetPath );
            if ( !isValid )
            {
                MessageBox.Show( "That instance name already exists or is invalid." );
                return;
            }

            //
            // Get the path to the template ZIP file.
            //
            var items = cbTemplates.ItemsSource as List<string>;
            string file = items[cbTemplates.SelectedIndex] + ".zip";
            string zipfile = System.IO.Path.Combine( Support.GetTemplatesPath(), file );

            //
            // Disable any UI controls that should not be available while deploying.
            //
            btnDeploy.IsEnabled = btnDelete.IsEnabled = false;

            //
            // Deploy the template as a new instance.
            //
            new Task( () =>
             {
                 //
                 // Extract the zip file to the target instance path.
                 //
                 Support.ExtractZipFile( zipfile, Path.Combine( targetPath, "RockWeb" ), ( progress ) =>
                 {
                     Dispatcher.Invoke( () =>
                     {
                         txtStatus.Text = string.Format( "Extracting {0:n0}%...", Math.Floor( progress * 100 ) );
                     } );
                 } );

                 //
                 // Update the UI to indicate that it is deployed.
                 //
                 Dispatcher.Invoke( () =>
                 {
                     txtStatus.Text = "Deployed";
                     UpdateState();

                     InstancesView.UpdateInstances();
                 } );
             } ).Start();
        }

        /// <summary>
        /// Delete the selected template from disk.
        /// </summary>
        /// <param name="sender">The object that has sent this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        private void btnDelete_Click( object sender, RoutedEventArgs e )
        {
            var result = MessageBox.Show( "Delete this template?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question );

            if ( result == MessageBoxResult.Yes )
            {
                var items = cbTemplates.ItemsSource as List<string>;

                string file = items[cbTemplates.SelectedIndex] + ".zip";
                string path = System.IO.Path.Combine( Support.GetTemplatesPath(), file );

                File.Delete( path );

                new Thread( LoadData ).Start();
            }
        }

        #endregion
    }
}
