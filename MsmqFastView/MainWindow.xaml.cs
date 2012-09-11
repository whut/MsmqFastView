using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using MsmqFastView.Infrastructure;
using MsmqFastView.Properties;

namespace MsmqFastView
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowModel();
        }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (!string.IsNullOrEmpty(Settings.Default.MainWindowPlacement))
            {
                this.SetPlacement(new JavaScriptSerializer().Deserialize<WindowNativeMethods.WINDOWPLACEMENT>(Settings.Default.MainWindowPlacement));
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            Settings.Default.MainWindowPlacement = new JavaScriptSerializer().Serialize(this.GetPlacement());
            Settings.Default.Save();
        }

        private void OnEditMachineNameIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                // must be deferred, otherwise Focus() will not work
                Task.Factory.StartNew(
                    () =>
                    {
                        this.tbCandidateMachineName.Focus();
                        this.tbCandidateMachineName.SelectAll();
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }
}
