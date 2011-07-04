using System;
using System.Runtime.Serialization;

namespace MsmqFastView.Infrastructure
{
    [Serializable]
    public class MsmqException : Exception
    {
        public MsmqException()
        {
        }

        public MsmqException(string message)
            : base(message)
        {
        }

        public MsmqException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MsmqException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new MsmqException(message);
            }
        }
    }
}
