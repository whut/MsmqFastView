﻿using System;
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
            this.ShowOnlyNonempty = true;
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
            this.OpenHomepage = new DelegateCommand(o =>
            {
                Process.Start("https://github.com/whut/MsmqFastView");
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ShowOnlyNonempty { get; set; }

        public DateTime LastRefresh { get; private set; }

        public string ApplicationVersion { get; private set; }

        public IEnumerable<QueueModel> Queues
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

        public ICommand OpenHomepage { get; private set; }

        private void InitializeQueues()
        {
            if (this.queues == null)
            {
                this.queues = new List<QueueModel>();
                try
                {
                    foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(Environment.MachineName)
                        .Where(q => !this.ShowOnlyNonempty || q.GetNumberOfMessages() != 0)
                        .OrderBy(mq => mq.QueueName))
                    {
                        this.queues.Add(new QueueModel(queue));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error during reading queues. Try refreshing queues list.\n"
                        + "\n"
                        + "Details:\n"
                        + ex.ToString(),
                        "MsmqFastView - Error during reading queues",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
            if (this.ShowOnlyNonempty && queue.GetNumberOfMessages() == 0)
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
