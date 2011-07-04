namespace MsmqFastView
{
    public class MessageDetailsModel
    {
        public MessageDetailsModel(string body, string correlationId)
        {
            this.Body = body;
            this.CorrelationId = correlationId;
        }

        public string Body { get; private set; }

        public string CorrelationId { get; private set; }
    }
}
