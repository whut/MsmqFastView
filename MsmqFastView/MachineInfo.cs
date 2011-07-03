using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Messaging;
using System.Windows.Input;
using MsmqFastView.Infrastructure;
using MsmqFastView.Infrastrucure;
using Rhino.ServiceBus.Msmq;

namespace MsmqFastView
{
    public class MachineInfo : INotifyPropertyChanged
    {
        private List<QueueInfo> queues;

        public MachineInfo()
        {
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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime LastRefresh { get; private set; }

        public bool ShowOnlyNonEmpty { get; set; }

        public List<QueueInfo> Queues
        {
            get
            {
                if (this.queues == null)
                {
                    this.queues = new List<QueueInfo>();
                    foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName)
                        .OrderBy(mq => mq.QueueName)
                        .SelectMany(q => this.GetQueueWithSubQueues(q)))
                    {
                        this.queues.Add(new QueueInfo(
                            queue.Path,
                            MsmqUtil.GetQueueUri(queue).AbsolutePath.Substring("/".Length)));
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
