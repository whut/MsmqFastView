using System;
using System.IO;
using System.Messaging;

namespace MsmqFastView
{
    public class MessageModel
    {
        private string queuePath;

        private MessageDetailsModel details;

        public MessageModel(string queuePath, string id, string label, DateTime sent, string responseQueue)
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

        public MessageDetailsModel Details
        {
            get
            {
                this.InitDetails();

                return this.details;
            }
        }

        private void InitDetails()
        {
            if (this.details == null)
            {
                using (var messageQueue = new MessageQueue(this.queuePath))
                {
                    messageQueue.MessageReadPropertyFilter.ClearAll();
                    messageQueue.MessageReadPropertyFilter.Body = true;
                    messageQueue.MessageReadPropertyFilter.CorrelationId = true;

                    try
                    {
                        using (var message = messageQueue.PeekById(this.Id))
                        {
                            this.details = new MessageDetailsModel(
                                new StreamReader(message.BodyStream).ReadToEnd(),
                                message.CorrelationId);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        this.details = new MessageDetailsModel(
                            "No message with the id " + this.Id + " exists. Probably it was consumed few moments ago.",
                            string.Empty);
                    }
                }
            }
        }
    }
}
