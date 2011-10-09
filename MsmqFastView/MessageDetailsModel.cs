using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using MsmqFastView.Extensions;

namespace MsmqFastView
{
    public class MessageDetailsModel
    {
        public MessageDetailsModel(string messageId, string queuePath)
        {
            var additionalDetails = new List<MessageDetail>();
            using (var messageQueue = new MessageQueue(queuePath))
            {
                messageQueue.MessageReadPropertyFilter.ClearAll();
                messageQueue.MessageReadPropertyFilter.Body = true;
                foreach (IMessageExtension messageExtension in MessageExtensions.GetMessageExtensions())
                {
                    messageExtension.CustomizeMessagePropertyFilter(messageQueue.MessageReadPropertyFilter);
                }

                try
                {
                    using (var message = messageQueue.PeekById(messageId))
                    {
                        this.Body = new StreamReader(message.BodyStream).ReadToEnd();
                        foreach (IMessageExtension messageExtension in MessageExtensions.GetMessageExtensions())
                        {
                            additionalDetails.AddRange(messageExtension.GetMessageDetails(message));
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    this.Body = "No message with the id " + messageId + " exists. Probably it was consumed few moments ago.\n"
                        + "Exception details: " + ex.ToString();
                }
                catch (Exception ex)
                {
                    this.Body = "Could not read details of message with id " + messageId + ".\n"
                        + "Exception details: " + ex.ToString();
                }

                this.AdditionalDetails = additionalDetails;
            }
        }

        public string Body { get; private set; }

        public IEnumerable<MessageDetail> AdditionalDetails { get; private set; }
    }
}
