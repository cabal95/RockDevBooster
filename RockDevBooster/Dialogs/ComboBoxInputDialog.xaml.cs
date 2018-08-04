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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace com.blueboxmoon.RockDevBooster.Dialogs
{
    /// <summary>
    /// Interaction logic for ComboBoxInputDialog.xaml
    /// </summary>
    public partial class ComboBoxInputDialog : Window
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the selected index.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return cbInput.SelectedIndex;
            }
            set
            {
                cbInput.SelectedIndex = value;
            }
        }

        /// <summary>
        /// Gets the selected value.
        /// </summary>
        /// <value>
        /// The selected value.
        /// </value>
        public string SelectedValue
        {
            get
            {
                if ( SelectedIndex == -1 )
                {
                    return null;
                }

                return Items[SelectedIndex];
            }
        }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public List<string> Items
        {
            get
            {
                return ( List<string> ) cbInput.ItemsSource;
            }
            set
            {
                cbInput.ItemsSource = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ComboBoxInputDialog"/> is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        public bool Required { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initialize a new combo box input dialog.
        /// </summary>
        public ComboBoxInputDialog()
        {
            InitializeComponent();

            cbInput.Focus();
        }

        /// <summary>
        /// Initialize a new combo box input dialog.
        /// </summary>
        /// <param name="owner">The user control that owns this dialog.</param>
        /// <param name="title">The title to display in the dialog.</param>
        public ComboBoxInputDialog( UserControl owner, string title )
            : this()
        {
            Owner = owner != null ? GetWindow( owner ) : Application.Current.MainWindow;
            Title = title;
        }

        #endregion

        private void btnCancel_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = false;
        }

        private void btnOK_Click( object sender, RoutedEventArgs e )
        {
            if ( Required && SelectedIndex == -1 )
            {
                return;
            }

            DialogResult = true;
        }
    }
}
