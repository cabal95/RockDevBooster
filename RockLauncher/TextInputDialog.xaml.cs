using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace com.blueboxmoon.RockLauncher
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        public string Text
        {
            get
            {
                return txtInput.Text;
            }
        }

        public TextInputDialog()
        {
            InitializeComponent();
        }

        private void btnOK_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
        }

        private void btnCancel_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
        }
    }
}
