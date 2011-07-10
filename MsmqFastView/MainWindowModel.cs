using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
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

        public DateTime LastRefresh { get; private set; }

        public bool ShowOnlyNonEmpty { get; set; }

        public string ApplicationVersion { get; set; }

        public List<QueueModel> Queues
        {
            get
            {
                if (this.queues == null)
                {
                    this.queues = new List<QueueModel>();
                    foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName)
                        .OrderBy(mq => mq.QueueName)
                        .SelectMany(q => this.GetQueueWithSubQueues(q)))
                    {
                        this.queues.Add(new QueueModel(
                            queue.Path));
                    }

                    this.LastRefresh = DateTime.Now;
                    this.PropertyChanged.Raise(this, "LastRefresh");
                }

                return this.queues;
            }
        }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }

        public ICommand PurgeAll { get; private set; }

        public ICommand OpenHomePage { get; private set; }

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
