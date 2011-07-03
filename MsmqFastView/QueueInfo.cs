using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Messaging;
using System.Windows.Input;
using MsmqFastView.Infrastructure;
using Rhino.ServiceBus.Msmq;

namespace MsmqFastView
{
    public class QueueInfo : INotifyPropertyChanged
    {
        private string path;

        private List<MessageInfo> messages;

        public QueueInfo(string path, string name)
        {
            this.path = path;
            this.Name = name;
            this.Refresh = new DelegateCommand(o =>
            {
                this.messages = null;
                this.PropertyChanged.Raise(this, "Messages");
            });
            this.Purge = new DelegateCommand(o =>
            {
                using (var messageQueue = new MessageQueue(this.path))
                {
                    messageQueue.Purge();
                }

                this.Refresh.Execute(o);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; private set; }

        public List<MessageInfo> Messages
        {
            get
            {
                if (this.messages == null)
                {
                    using (var messageQueue = new MessageQueue(this.path))
                    {
                        messageQueue.MessageReadPropertyFilter.ClearAll();
                        messageQueue.MessageReadPropertyFilter.Id = true;
                        messageQueue.MessageReadPropertyFilter.Label = true;
                        messageQueue.MessageReadPropertyFilter.SentTime = true;
                        messageQueue.MessageReadPropertyFilter.ResponseQueue = true;

                        this.messages = messageQueue
                            .Cast<Message>()
                            .Reverse()
                            .Select(m => new MessageInfo(
                                this.path,
                                m.Id,
                                m.Label,
                                m.SentTime,
                                MsmqUtil.GetQueueUri(m.ResponseQueue).ToString().Substring("msmq://".Length)))
                            .ToList();
                    }
                }

                return this.messages;
            }
        }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }
    }
}
