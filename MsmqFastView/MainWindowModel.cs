using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Windows;
using System.Windows.Input;
using MsmqFastView.Infrastructure;

namespace MsmqFastView
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private List<QueueModel> queues;

        public MainWindowModel()
        {
            this.ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.ShowOnlyNonEmpty = true;
            this.Refresh = new DelegateCommand(o =>
            {
                this.queues = null;
                this.PropertyChanged.Raise(this, "Queues");
            });
            this.Purge = new DelegateCommand(o =>
            {
                foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName))
                {
                    queue.Purge();
                }

                this.Refresh.Execute(o);
            });
            this.PurgeAll = new DelegateCommand(o =>
            {
                foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName)
                    .SelectMany(q => GetQueueWithSubQueues(q)))
                {
                    queue.Purge();
                }

                this.Refresh.Execute(o);
            });
            this.OpenHomePage = new DelegateCommand(o =>
            {
                Process.Start("https://github.com/whut/MsmqFastView");
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ShowOnlyNonEmpty { get; set; }

        public DateTime LastRefresh { get; private set; }

        public string ApplicationVersion { get; private set; }

        public List<QueueModel> Queues
        {
            get
            {
                this.InitializeQueues();

                return this.queues;
            }
        }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }

        public ICommand PurgeAll { get; private set; }

        public ICommand OpenHomePage { get; private set; }

        private void InitializeQueues()
        {
            if (this.queues == null)
            {
                this.queues = new List<QueueModel>();
                try
                {
                    foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName)
                        .OrderBy(mq => mq.QueueName)
                        .SelectMany(q => this.GetQueueWithSubQueues(q)))
                    {
                        this.queues.Add(new QueueModel(
                            queue.Path));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error during reading queues. Try refreshing queues list.\n"
                        + "\n"
                        + "Details:\n"
                        + ex.ToString(),
                        "Error during reading queues",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    throw;
                }
                finally
                {
                    this.LastRefresh = DateTime.Now;
                    this.PropertyChanged.Raise(this, "LastRefresh");
                }
            }
        }

        private IEnumerable<MessageQueue> GetQueueWithSubQueues(MessageQueue queue)
        {
            if (this.ShowOnlyNonEmpty && queue.GetNumberOfMessages() == 0)
            {
                yield break;
            }

            yield return queue;

            if (queue.GetNumberOfSubqueues() > 0)
            {
                foreach (string subQueueName in queue.GetSubqueueNames())
                {
                    using (MessageQueue subQueue = new MessageQueue(queue.Path + ";" + subQueueName))
                    {
                        yield return subQueue;
                    }
                }
            }
        }
    }
}
