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

namespace com.blueboxmoon.RockDevBooster.Dialogs
{
    /// <summary>
    /// Interaction logic for PendingUpdateDialog.xaml
    /// </summary>
    public partial class PendingUpdateDialog : Window
    {
        public PendingUpdateDialog()
        {
            InitializeComponent();
        }

        #region Events

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
        }

        /// <summary>
        /// Handles the Click event of the btnOK control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void btnOK_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
        }

        #endregion
    }
}
