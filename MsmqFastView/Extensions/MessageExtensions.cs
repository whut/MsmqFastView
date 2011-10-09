using System.Collections.Generic;

namespace MsmqFastView.Extensions
{
    public static class MessageExtensions
    {
        private static IMessageExtension[] messageExtensions = new[] { new RhinoServiceBusMessageExtension() };

        public static IEnumerable<IMessageExtension> GetMessageExtensions()
        {
            return messageExtensions;
        }
    }
}
