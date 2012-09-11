using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Reflection;
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
            this.ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.ShowOnlyNonempty = true;
            this.Machine = new MachineModel();
			this.Machine.PropertyChanged += (sender, e) => { if (e.PropertyName == "MachineName") { this.OnMachineNameChanged(); } };
            this.Refresh = new DelegateCommand(o =>
            {
                this.queues = null;
                this.PropertyChanged.Raise(this, "Queues");
            });
            this.Purge = new DelegateCommand(o =>
            {
                foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(this.MachineName))
                {
                    queue.Purge();
                }

                this.Refresh.Execute(o);
            });
            this.PurgeAll = new DelegateCommand(o =>
            {
                foreach (MessageQueue queue in MessageQueue.GetPrivateQueuesByMachine(this.MachineName)
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

		public string MachineName
		{
			get
			{
				return this.Machine.MachineName;
			}
		}

		public MachineModel Machine { get; private set; }

        public string Title
        {
            get
            {
                return "MsmqFastView - " + this.MachineName;
            }
        }

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
                    IEnumerable<MessageQueue> queueSource = MessageQueue.GetPrivateQueuesByMachine(this.MachineName);

                    queueSource = ApplyNet45WorkaroundIfNeeded(queueSource);

                    foreach (MessageQueue queue in queueSource
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
                        "Error during reading queues",
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

        private static IEnumerable<MessageQueue> ApplyNet45WorkaroundIfNeeded(IEnumerable<MessageQueue> queueSource)
        {
            // 4.0 RTM -> 4.0.30319.1
            // 4.0.1 -> 4.0.30319.232
            // 4.0.2 -> 4.0.30319.245
            // 4.0.3 -> 4.0.30319.276
            // 4.5 Dev -> 4.0.30319.17020
            // 4.5 Beta -> 4.0.30319.17379
            // 4.5 RC -> 4.0.30319.17626
            // 4.5 RTM -> 4.0.30319.17929

            // we want to do it only on 4.5 (all releases)
            if (Environment.Version.Major == 4
                && Environment.Version.Minor == 0
                && Environment.Version.Build == 30319
                && Environment.Version.Revision >= 17000)
            {
                queueSource = queueSource.Select(mq => WorkAroundNet45FormatNameBug(mq));
            }
            return queueSource;
        }

        private static MessageQueue WorkAroundNet45FormatNameBug(MessageQueue mq)
        {
            if (mq == null)
            {
                return mq;
            }

            // In .NET 2.0 and 4.0, if queue is constructed from format name ("FormatName:...."), 
            // accessing the FormatName property results in a simple substring operation (to trim the "FormatName:" prefix).

            // In .NET 4.5, some overzealous Microsoft programmer thought it was insufficient,
            // and if the private queuePath field is set, the FormatName property calls ResolveFormatNameFromQueuePath,
            // which calls the native MQPathNameToFormatName API, which does not support remote paths on workgroup (non-domain joined) machines.
            // If the private queuePath field is NOT set, the old string manipulation code path is used.
            // The computed format name is then cached in a field.

            // (MessageQueue.GetPrivateQueuesByMachine DOES set this field. The MessageQueue constructor does NOT.)

            // The workaround is to clear the queuePath field, then access FormatName to cause the format name to be cached. 
            // Afterwards, queuePath may be restored.
            // This effectively simulates .NET 4.0 behavior.

            var field = mq.GetType().GetMember("queuePath", MemberTypes.Field, BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault() as FieldInfo;

            // no field? apparently we are running on a future framework; the internals have changed -> don't touch anything
            if (field == null)
            {
                return mq;
            }

            // save existing queuePath value
            var savedPath = field.GetValue(mq);
            if (savedPath == null)
            {
                // huh? nothing we can do
                return mq;
            }

            // clear the field
            field.SetValue(mq, null);
            
            // trigger caching of format name
            var formatName = mq.FormatName;

            // restore queuePath value
            field.SetValue(mq, savedPath);
            return mq;
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

		private void OnMachineNameChanged()
		{
			this.Refresh.Execute(null);
			this.PropertyChanged.Raise(this, "Title");
		}
	}
}
