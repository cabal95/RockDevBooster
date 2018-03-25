using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using com.blueboxmoon.RockDevBooster.Properties;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing( CancelEventArgs e )
        {
            Settings.Default.MainWindowPlacement = WindowPlacement.GetPlacement( new WindowInteropHelper( this ).Handle );
            Settings.Default.Save();
        }

        protected override void OnSourceInitialized( EventArgs e )
        {
            base.OnSourceInitialized( e );
            WindowPlacement.SetPlacement( new WindowInteropHelper( this ).Handle, Settings.Default.MainWindowPlacement );
        }
    }
}
