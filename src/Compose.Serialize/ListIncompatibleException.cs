using System.Runtime.Serialization;

namespace Compose.Serialize
{
    [Serializable]
    internal class ListIncompatibleException : Exception
    {
        public ListIncompatibleException()
        {
        }

        public ListIncompatibleException(string? message) : base(message)
        {
        }

        public ListIncompatibleException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ListIncompatibleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}