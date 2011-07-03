using System.ComponentModel;

namespace MsmqFastView.Infrastructure
{
    public static class NotifyPropertyChangedExtensions
    {
        public static void Raise(this PropertyChangedEventHandler propertyChangedEventHandler, object sender, string propertyName)
        {
            PropertyChangedEventHandler handler = propertyChangedEventHandler;
            if (handler != null)
            {
                handler(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
