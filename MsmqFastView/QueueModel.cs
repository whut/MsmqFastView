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
        private const string JournalQueueName = @"JOURNAL";

        private string path;

        private List<MessageModel> messages;

        public QueueModel(MessageQueue queue)
            : this()
        {
            this.path = queue.Path;
            List<QueueModel> subqueues = new List<QueueModel>();

            var messageCount = queue.GetNumberOfMessages();
            this.Name = GetFriendlyName(queue) + (0 < messageCount ? string.Format(" ({0})", messageCount) : null);

            // queue properties (e.g. UseJournalQueue) are only accessible from the local machine
            if (!queue.MachineName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || queue.UseJournalQueue)
            {
                subqueues.Add(new QueueModel(queue, JournalQueueName));
            }

            if (queue.GetNumberOfSubqueues() > 0)
            {
                foreach (string subQueueName in queue.GetSubqueueNames())
                {
                    subqueues.Add(new QueueModel(queue, subQueueName));
                }
            }

            this.SubQueues = subqueues;
        }

        public QueueModel(MessageQueue queue, string subQueueName)
            : this()
        {
            this.path = queue.Path + ";" + subQueueName;

            if (subQueueName.Equals(JournalQueueName, StringComparison.OrdinalIgnoreCase))
            {
                var messageCount = MsmqExtensions.GetNumberOfMessagesInJournal(queue.MachineName, queue.FormatName);
                this.Name = "Journal" + (0 < messageCount ? string.Format(" ({0})", messageCount) : null);
            }
            else
            {
                this.Name = subQueueName;
            }
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
            var formatName = queue.FormatName;

            // QueueName is unavailable on remote queue with DIRECT FormatName when the following conditions are met:
            // * this machine is not joined to a domain, so MSMQ path translation mechanisms work only on local queues
            // * the MessageQueue object does not have its private field queuePath set 
            //   (note: queues obtained from GetPrivateQueuesByMachine DO have this field set, but those returned by e.g. Message.ResponseQueue, or constructed from format name string, DO NOT)
            // in case of exception, better to display raw FormatName than fail to display the entire message list
            try
            {
                string prefix = "private$\\";
                if (queue.QueueName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return queue.QueueName.Substring(prefix.Length);
                }

                return queue.QueueName;
            }
            catch (MessageQueueException)
            {
                return queue.FormatName;
            }
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
                        messageQueue.MessageReadPropertyFilter.MessageType = true;
                        messageQueue.MessageReadPropertyFilter.Acknowledgment = true;

                        this.messages = messageQueue
                            .Cast<Message>()
                            .Reverse()
                            .Select(m => new MessageModel(
                                this.path,
                                m.Id,
                                m.Label,
                                m.SentTime,
                                m.ResponseQueue != null ? GetFriendlyName(m.ResponseQueue) : string.Empty,
                                m.CorrelationId,
                                m.MessageType.ToString(),
                                m.Acknowledgment.ToString()))
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
