namespace MsmqFastView.Extensions
{
    public class MessageDetail
    {
        public MessageDetail(string group, string name, string value)
        {
            this.Group = group;
            this.Name = name;
            this.Value = value;
        }

        public string Group { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
