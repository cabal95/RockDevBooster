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
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the user-entered text.
        /// </summary>
        public string Text
        {
            get
            {
                return txtInput.Text;
            }
            set
            {
                txtInput.Text = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new text input dialog.
        /// </summary>
        public TextInputDialog()
        {
            InitializeComponent();

            txtInput.Focus();
        }

        /// <summary>
        /// Initialize a new text input dialog.
        /// </summary>
        /// <param name="owner">The user control that owns this dialog.</param>
        /// <param name="title">The title to display in the dialog.</param>
        public TextInputDialog( UserControl owner, string title )
            : this()
        {
            Owner = Window.GetWindow( owner );
            Title = title;
        }

        #endregion

        private void btnCancel_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
        }

        private void btnOK_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
        }
    }
}
