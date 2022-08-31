using System.Runtime.Serialization;

namespace Compose.Serialize
{
    [Serializable]
    internal class ObjectNameCollisionException : Exception
    {
        public ObjectNameCollisionException()
        {
        }

        public ObjectNameCollisionException(string? message) : base(message)
        {
        }

        public ObjectNameCollisionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ObjectNameCollisionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}