using System;
using System.IO;
using System.Messaging;

namespace MsmqFastView
{
    public class MessageInfo
    {
        private string queuePath;

        private MessageDetailsInfo details;

        public MessageInfo(string queuePath, string id, string label, DateTime sent, string responseQueue)
        {
            this.queuePath = queuePath;
            this.Id = id;
            this.Label = label;
            this.Sent = sent;
            this.ResponseQueue = responseQueue;
        }

        public string Id { get; private set; }

        public string Label { get; private set; }

        public DateTime Sent { get; private set; }

        public string ResponseQueue { get; private set; }

        public MessageDetailsInfo Details
        {
            get
            {
                if (this.details == null)
                {
                    using (var messageQueue = new MessageQueue(this.queuePath))
                    {
                        messageQueue.MessageReadPropertyFilter.ClearAll();
                        messageQueue.MessageReadPropertyFilter.Body = true;
                        messageQueue.MessageReadPropertyFilter.CorrelationId = true;

                        using (var message = messageQueue.PeekById(this.Id))
                        {
                            this.details = new MessageDetailsInfo(
                                new StreamReader(message.BodyStream).ReadToEnd(),
                                message.CorrelationId);
                        }
                    }
                }

                return this.details;
            }
        }
    }
}
