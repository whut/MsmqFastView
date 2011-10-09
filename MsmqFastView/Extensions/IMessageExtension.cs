using System.Collections.Generic;
using System.Messaging;

namespace MsmqFastView.Extensions
{
    public interface IMessageExtension
    {
        void CustomizeMessagePropertyFilter(MessagePropertyFilter messagePropertyFilter);

        IEnumerable<MessageDetail> GetMessageDetails(Message message);
    }
}
