using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace com.blueboxmoon.RockDevBooster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void Application_Exception( object sender, DispatcherUnhandledExceptionEventArgs e )
        {
            MessageBox.Show( e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error );
            e.Handled = true;
            Dispatcher.InvokeShutdown();
        }
    }
}
