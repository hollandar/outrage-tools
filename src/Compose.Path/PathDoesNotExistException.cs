using System.Runtime.Serialization;

namespace Compose.Path
{
    [Serializable]
    internal class PathDoesNotExistException : Exception
    {
        public PathDoesNotExistException()
        {
        }

        public PathDoesNotExistException(string? message) : base(message)
        {
        }

        public PathDoesNotExistException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected PathDoesNotExistException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}