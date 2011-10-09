using System;

namespace MsmqFastView
{
    public class MessageModel
    {
        private string queuePath;

        private MessageDetailsModel details;

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

        public MessageDetailsModel Details
        {
            get
            {
                if (this.details == null)
                {
                    this.details = new MessageDetailsModel(this.Id, this.queuePath);
                }

                return this.details;
            }
        }
    }
}
