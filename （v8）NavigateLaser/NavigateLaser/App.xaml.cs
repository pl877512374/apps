using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace NavigateLaser
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MyBootstrapper bootstrapper = new MyBootstrapper();
            bootstrapper.Run();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var comException = e.Exception as System.Runtime.InteropServices.COMException;
            if (comException != null && comException.ErrorCode == -2147221040)
                e.Handled = true;
        }
    }
}
