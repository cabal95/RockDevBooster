using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

using com.blueboxmoon.RockDevBooster.Properties;

namespace com.blueboxmoon.RockDevBooster.Views
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesView : UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();

            if ( !DesignerProperties.GetIsInDesignMode( this ) )
            {
                var vsList = VisualStudioInstall.GetVisualStudioInstances();
                var vs = VisualStudioInstall.GetDefaultInstall();

                cbVisualStudio.ItemsSource = vsList;

                if ( vs != null )
                {
                    int selectedIndex = vsList.IndexOf( vsList.Where( v => v.Path == vs.Path ).FirstOrDefault() );
                    cbVisualStudio.SelectedIndex = selectedIndex;
                }
            }
        }

        private void cbVisualStudio_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            var vsList = cbVisualStudio.ItemsSource as List<VisualStudioInstall>;
            var vs = vsList[cbVisualStudio.SelectedIndex];
            
            Settings.Default.VisualStudioVersion = vs.Path;
            Settings.Default.Save();
        }
    }
}
