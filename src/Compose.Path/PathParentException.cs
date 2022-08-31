using System.Runtime.Serialization;

namespace Compose.Path
{
    [Serializable]
    internal class PathParentException : Exception
    {
        public PathParentException()
        {
        }

        public PathParentException(string? message) : base(message)
        {
        }

        public PathParentException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected PathParentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}