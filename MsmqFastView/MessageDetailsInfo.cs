namespace MsmqFastView
{
    public class MessageDetailsInfo
    {
        public MessageDetailsInfo(string body, string correlationId)
        {
            this.Body = body;
            this.CorrelationId = correlationId;
        }

        public string Body { get; private set; }

        public string CorrelationId { get; private set; }
    }
}
