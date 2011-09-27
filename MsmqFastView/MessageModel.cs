using System;
using System.IO;
using System.Messaging;

namespace MsmqFastView
{
    public class MessageModel
    {
        private string queuePath;

        private string body;

        public MessageModel(string queuePath, string id, string label, DateTime sent, string responseQueue, string correlationId)
        {
            this.queuePath = queuePath;
            this.Id = id;
            this.Label = label;
            this.Sent = sent;
            this.ResponseQueue = responseQueue;
            this.CorrelationId = correlationId;
        }

        public string Id { get; private set; }

        public string Label { get; private set; }

        public DateTime Sent { get; private set; }

        public string ResponseQueue { get; private set; }

        public string CorrelationId { get; private set; }

        public string Body
        {
            get
            {
                this.InitBody();

                return this.body;
            }
        }

        private void InitBody()
        {
            if (this.body == null)
            {
                using (var messageQueue = new MessageQueue(this.queuePath))
                {
                    messageQueue.MessageReadPropertyFilter.ClearAll();
                    messageQueue.MessageReadPropertyFilter.Body = true;

                    try
                    {
                        using (var message = messageQueue.PeekById(this.Id))
                        {
                            this.body = new StreamReader(message.BodyStream).ReadToEnd();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        this.body = "No message with the id " + this.Id + " exists. Probably it was consumed few moments ago.";
                    }
                }
            }
        }
    }
}
