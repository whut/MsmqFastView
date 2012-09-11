using System;
using System.ComponentModel;
using System.Messaging;
using System.Windows;
using System.Windows.Input;
using MsmqFastView.Infrastructure;

namespace MsmqFastView
{
    public class MachineModel : INotifyPropertyChanged
    {
        private string machineName;

        private string candidateMachineName;

        private bool editingMachineName;

        public MachineModel()
        {
            this.MachineName = Environment.MachineName;

            this.ChangeMachine = new DelegateCommand(_ => this.OnChangeMachine());
            this.ConnectToMachine = new DelegateCommand(_ => this.CanConnectToMachine(), _ => this.OnConnectToMachine());
            this.CancelChangeMachine = new DelegateCommand(_ => this.OnCancelChangeMachine());
        }

        public string MachineName
        {
            get
            {
                return this.machineName;
            }
            private set
            {
                this.machineName = value;
                this.PropertyChanged.Raise(this, "MachineName");
            }
        }

        public string CandidateMachineName
        {
            get
            {
                return this.candidateMachineName;
            }
            set
            {
                this.candidateMachineName = value;
                this.PropertyChanged.Raise(this, "CandidateMachineName");
                this.ConnectToMachine.RaiseCanExecuteChanged();
            }
        }

        public Visibility MachineNameVisibility
        {
            get
            {
                return this.editingMachineName ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility EditMachineNameVisibility
        {
            get
            {
                return this.editingMachineName ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ICommand ChangeMachine { get; private set; }

        public DelegateCommand ConnectToMachine { get; private set; }

        public ICommand CancelChangeMachine { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ToggleMachineNameEditor(bool editing)
        {
            this.editingMachineName = editing;
            this.PropertyChanged.Raise(this, "MachineNameVisibility");
            this.PropertyChanged.Raise(this, "EditMachineNameVisibility");
        }

        private void OnChangeMachine()
        {
            this.CandidateMachineName = this.MachineName;
            this.ToggleMachineNameEditor(true);
        }

        private void OnCancelChangeMachine()
        {
            this.ToggleMachineNameEditor(false);
        }

        private bool CanConnectToMachine()
        {
            return !string.IsNullOrEmpty(this.CandidateMachineName);
        }

        private void OnConnectToMachine()
        {
            try
            {
                MessageQueue.GetPrivateQueuesByMachine(this.CandidateMachineName);
            }
            catch (MessageQueueException ex)
            {
                MessageBox.Show(
                    "Unable to obtain queue list from machine " + this.CandidateMachineName + ". Make sure the machine name is correct. To check if the machine is properly configured for remote MSMQ administration, try connecting to it using the MSMQ node in Computer Management/Server Manager.\n"
                    + "\n"
                    + "Details:\n"
                    + ex.ToString(),
                    "Error during reading queues",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            this.MachineName = this.CandidateMachineName;
            this.ToggleMachineNameEditor(false);
        }
    }
}
