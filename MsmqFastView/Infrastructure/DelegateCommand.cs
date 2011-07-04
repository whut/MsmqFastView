using System;
using System.Windows.Input;

namespace MsmqFastView.Infrastructure
{
    public class DelegateCommand : ICommand
    {
        private Predicate<object> canExecuteDelegate;

        private Action<object> executeDelegate;

        public DelegateCommand(Predicate<object> canExecuteDelegate, Action<object> executeDelegate)
        {
            this.canExecuteDelegate = canExecuteDelegate;
            this.executeDelegate = executeDelegate;
        }

        public DelegateCommand(Action<object> executeDelegate)
            : this(o => true, executeDelegate)
        {
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return this.canExecuteDelegate(parameter);
        }

        public void Execute(object parameter)
        {
            this.executeDelegate(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
