using System;
using System.Windows;
using MsmqFastView.Infrastructure;

namespace MsmqFastView
{
    public partial class App : Application
    {
        public App()
        {
            if (!IsMsmqInstalled())
            {
                MessageBox.Show("MSMQ not installed. Please install MSMQ and then restart MsmqFastView.\n\n"
                        + "You can install MSMQ using Control Panel -> Programs and Features -> Turn Windows features on or off. Select Microsoft Messsage Queue (MSMQ) Server and click Ok.",
                        "MsmqFastView - MSMQ not installed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                this.Shutdown(1);
            }
        }

        private bool IsMsmqInstalled()
        {
            try
            {
                MsmqNativeMethods.MQMgmtGetInfo(Environment.MachineName, null, null);
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch { }

            return true;
        }
    }
}
