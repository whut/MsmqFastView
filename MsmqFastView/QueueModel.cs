using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Messaging;
using System.Windows;
using System.Windows.Input;
using MsmqFastView.Infrastructure;

namespace MsmqFastView
{
    public class QueueModel : INotifyPropertyChanged
    {
        private const string JournalQueueSuffix = @"\Journal$";

        private string path;

        private List<MessageModel> messages;

        public QueueModel(MessageQueue queue)
            : this()
        {
            this.path = queue.Path;
            List<QueueModel> subqueues = new List<QueueModel>();
            if (!queue.Path.EndsWith(JournalQueueSuffix))
            {
                var messageCount = queue.GetNumberOfMessages();
                this.Name = GetFriendlyName(queue) + (0 < messageCount ? string.Format(" ({0})", messageCount) : null);

                // journal queues are only accessible from the local machine (TODO: confirm)
                if (queue.MachineName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) && queue.UseJournalQueue)
                {
                    subqueues.Add(new QueueModel(new MessageQueue(@".\" + queue.QueueName + JournalQueueSuffix)));
                }

                if (queue.GetNumberOfSubqueues() > 0)
                {
                    foreach (string subQueueName in queue.GetSubqueueNames())
                    {
                        subqueues.Add(new QueueModel(queue, subQueueName));
                    }
                }
            }
            else
            {
                this.Name = "Journal";
            }

            this.SubQueues = subqueues;
        }

        public QueueModel(MessageQueue queue, string subQueueName)
            : this()
        {
            this.path = queue.Path + ";" + subQueueName;
            this.Name = subQueueName;
        }

        private QueueModel()
        {
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

        public IEnumerable<MessageModel> Messages
        {
            get
            {
                this.InitMessages();

                return this.messages;
            }
        }

        public IEnumerable<QueueModel> SubQueues { get; set; }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }

        private static string GetFriendlyName(MessageQueue queue)
        {
            string prefix = "private$\\";
            if (queue.QueueName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return queue.QueueName.Substring(prefix.Length);
            }

            return queue.QueueName;
        }

        private void InitMessages()
        {
            if (this.messages == null)
            {
                try
                {
                    using (var messageQueue = new MessageQueue(this.path))
                    {
                        messageQueue.MessageReadPropertyFilter.ClearAll();
                        messageQueue.MessageReadPropertyFilter.Id = true;
                        messageQueue.MessageReadPropertyFilter.Label = true;
                        messageQueue.MessageReadPropertyFilter.SentTime = true;
                        messageQueue.MessageReadPropertyFilter.ResponseQueue = true;
                        messageQueue.MessageReadPropertyFilter.CorrelationId = true;

                        this.messages = messageQueue
                            .Cast<Message>()
                            .Reverse()
                            .Select(m => new MessageModel(
                                this.path,
                                m.Id,
                                m.Label,
                                m.SentTime,
                                m.ResponseQueue != null ? GetFriendlyName(m.ResponseQueue) : string.Empty,
                                m.CorrelationId))
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    this.messages = new List<MessageModel>();

                    MessageBox.Show(
                        "Error during reading messages. Try refreshing queues list.\n"
                        + "\n"
                        + "Details:\n"
                        + ex.ToString(),
                        "Error during reading messages",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}
