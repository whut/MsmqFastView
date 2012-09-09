using System;
using System.Windows;

namespace MsmqFastView
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                string.Concat(
                    "An unexpected error has occured. Please notify the developers.",
                    Environment.NewLine,
                    Environment.NewLine,
                    "Details:",
                    Environment.NewLine,
                    e.ExceptionObject.ToString()), 
                "Unhandled exception");
        }
    }
}
