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
        private const string PathPrefix = "FORMATNAME:DIRECT=OS:";

        private string path;

        private List<MessageModel> messages;

        public QueueModel(string path)
        {
            this.path = path;
            this.Name = GetFriendlyName(path);
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

        public List<MessageModel> Messages
        {
            get
            {
                this.InitMessages();

                return this.messages;
            }
        }

        public ICommand Refresh { get; private set; }

        public ICommand Purge { get; private set; }

        private static string GetFriendlyName(string queuePath)
        {
            if (queuePath.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return queuePath.Substring(PathPrefix.Length);
            }

            return queuePath;
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

                        this.messages = messageQueue
                            .Cast<Message>()
                            .Reverse()
                            .Select(m => new MessageModel(
                                this.path,
                                m.Id,
                                m.Label,
                                m.SentTime,
                                m.ResponseQueue != null ? GetFriendlyName(m.ResponseQueue.Path) : string.Empty))
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    this.messages = new List<MessageModel>();

                    MessageBox.Show(
                        "Error during reading messages. Try refreshing messages list.\n"
                        + "\n"
                        + "Details:\n"
                        + ex.ToString(),
                        "Error during reading messages",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    throw;
                }
            }
        }
    }
}
