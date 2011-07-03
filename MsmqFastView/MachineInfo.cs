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
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime LastRefresh { get; private set; }

        public List<QueueInfo> Queues
        {
            get
            {
                if (this.queues == null)
                {
                    this.queues = new List<QueueInfo>();
                    foreach (MessageQueue queue in MessageQueue
                        .GetPrivateQueuesByMachine(Environment.MachineName)
                        .OrderBy(mq => mq.QueueName))
                    {
                        using (var mq = new MessageQueue(queue.Path))
                        {
                            this.queues.Add(new QueueInfo(queue.Path, MsmqUtil.GetQueueUri(queue).AbsolutePath.Substring("/".Length)));
                            if (NativeWrapper.GetNumberOfSubqueues(queue.FormatName) > 0)
                            {
                                foreach (string subQueueName in NativeWrapper.GetSubqueueNames(queue.FormatName))
                                {
                                    using (var subQueue = new MessageQueue(queue.Path + ";" + subQueueName))
                                    {
                                        this.queues.Add(new QueueInfo(
                                            subQueue.Path,
                                            MsmqUtil.GetQueueUri(subQueue).AbsolutePath.Substring("/".Length).Replace(";", "#")));
                                    }
                                }
                            }
                        }
                    }

                    this.LastRefresh = DateTime.Now;
                    this.PropertyChanged.Raise(this, "LastRefresh");
                }

                return this.queues;
            }
        }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }
    }
}
