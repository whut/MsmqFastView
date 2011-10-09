using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Messaging;

namespace MsmqFastView.Extensions
{
    /// <summary>
    /// Based on Rhino Service Bus version 2.2.1
    /// </summary>
    public class RhinoServiceBusMessageExtension : IMessageExtension
    {
        private const string RsbMessageDetails = "Rhino Service Bus message details";
        private const string RsbMessageHeaders = "Rhino Service Bus message headers";

        // From Rhino.ServiceBus.Transport.MessageType
        private enum MessageType
        {
            StandardMessage = 0,
            ShutDownMessageMarker = 3,
            AdministrativeMessageMarker = 4,
            TimeoutMessageMarker = 5,
            LoadBalancerMessageMarker = 6,
            MoveMessageMarker = 7
        }

        public void CustomizeMessagePropertyFilter(MessagePropertyFilter messagePropertyFilter)
        {
            messagePropertyFilter.Extension = true;
            messagePropertyFilter.AppSpecific = true;
        }

        public IEnumerable<MessageDetail> GetMessageDetails(Message message)
        {
            if (!IsRsbMessage(message))
            {
                return Enumerable.Empty<MessageDetail>();
            }

            try
            {
                List<MessageDetail> messageDetails = new List<MessageDetail>()
                {
                    new MessageDetail(RsbMessageDetails, "Id", GetMessageId(message.Extension).ToString()),
                    new MessageDetail(RsbMessageDetails, "Message type", ((MessageType)message.AppSpecific).ToString())
                };

                if (message.Extension.Length > 16)
                {
                    DateTime timeout = GetTimeout(message.Extension);
                    if (timeout != DateTime.MinValue)
                    {
                        messageDetails.Add(new MessageDetail(RsbMessageDetails, "Process again at", timeout.ToString()));
                    }
                }

                NameValueCollection headers = DeserializeHeaders(message.Extension);
                foreach (string key in headers.AllKeys)
                {
                    messageDetails.Add(new MessageDetail(RsbMessageHeaders, key, headers[key]));
                }

                return messageDetails;
            }
            catch (Exception ex)
            {
                return new MessageDetail[] 
                {
                    new MessageDetail(RsbMessageDetails, "Error", "Could not extract message details because of an error."),
                    new MessageDetail(RsbMessageDetails, "Details", ex.ToString())
                };
            }
        }

        private static bool IsRsbMessage(Message message)
        {
            return message.Extension.Length >= 16 && message.AppSpecific >= 0 && message.AppSpecific <= 7;
        }

        // From Rhino.ServiceBus.Msmq.GetMessageId
        private static Guid GetMessageId(byte[] messageExtension)
        {
            var guid = new byte[16];
            Buffer.BlockCopy(messageExtension, 0, guid, 0, 16);
            return new Guid(guid);
        }

        // From Rhino.ServiceBus.Msmq.GetTimeoutMessages
        private static DateTime GetTimeout(byte[] messageExtension)
        {
            return DateTime.FromBinary(BitConverter.ToInt64(messageExtension, 16));
        }

        // From Rhino.ServiceBus.Util.SerializationExtensions.DeserializeHeaders
        private static NameValueCollection DeserializeHeaders(byte[] messageExtension)
        {
            var headers = new NameValueCollection();
            if (messageExtension.Length > 24)
            {
                using (var ms = new MemoryStream(messageExtension, 24, messageExtension.Length - 24))
                using (var binaryReader = new BinaryReader(ms))
                {
                    var headerCount = binaryReader.ReadInt32();
                    for (int i = 0; i < headerCount; ++i)
                    {
                        headers.Add(binaryReader.ReadString(), binaryReader.ReadString());
                    }
                }
            }

            return headers;
        }
    }
}
