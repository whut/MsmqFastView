using System.Windows;

namespace MsmqFastView
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowModel();
        }
    }
}
