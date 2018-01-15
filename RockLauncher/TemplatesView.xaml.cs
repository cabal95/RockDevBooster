using System;
using System.Collections.Generic;
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

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for TemplatesView.xaml
    /// </summary>
    public partial class TemplatesView : UserControl
    {
        static private TemplatesView DefaultTemplatesView;

        public TemplatesView()
        {
            DefaultTemplatesView = this;

            InitializeComponent();

            txtStatus.Text = string.Empty;
            UpdateState();

            new Thread( LoadData ).Start();
        }

        static public void UpdateTemplates()
        {
            DefaultTemplatesView.btnDeploy.IsEnabled = false;
            DefaultTemplatesView.btnDelete.IsEnabled = false;

            new Thread( DefaultTemplatesView.LoadData ).Start();
        }

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

        protected void UpdateState()
        {
            bool buttonsEnabled = cbTemplates.SelectedIndex != -1;

            btnDelete.IsEnabled = buttonsEnabled;
            btnDeploy.IsEnabled = buttonsEnabled;
        }

        private void ComboBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            UpdateState();
        }

        private void btnDeploy_Click( object sender, RoutedEventArgs e )
        {
            bool isValid = false;
            string targetPath;

            targetPath = System.IO.Path.Combine( Support.GetInstancesPath(), txtName.Text );
            isValid = !string.IsNullOrWhiteSpace( txtName.Text ) && !Directory.Exists( targetPath );

            if ( !isValid )
            {
                MessageBox.Show( "That instance name already exists or is invalid." );
                return;
            }

            var items = cbTemplates.ItemsSource as List<string>;
            string file = items[cbTemplates.SelectedIndex] + ".zip";
            string zipfile = System.IO.Path.Combine( Support.GetTemplatesPath(), file );

            btnDeploy.IsEnabled = btnDelete.IsEnabled = false;
            new Thread( () =>
             {
                 Support.ExtractZipFile( zipfile, targetPath, ( progress ) =>
                 {
                     Dispatcher.Invoke( () =>
                     {
                         txtStatus.Text = string.Format( "Extracting {0:n0}%...", Math.Floor( progress * 100 ) );
                     } );
                 } );

                 Dispatcher.Invoke( () =>
                 {
                     txtStatus.Text = "Deployed";
                     UpdateState();

                     InstancesView.UpdateInstances();
                 } );
             } ).Start();
        }

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
    }
}
