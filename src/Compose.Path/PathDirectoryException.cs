using System.Runtime.Serialization;

namespace Compose.Path
{
    [Serializable]
    internal class PathDirectoryException : Exception
    {
        public PathDirectoryException()
        {
        }

        public PathDirectoryException(string? message) : base(message)
        {
        }

        public PathDirectoryException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected PathDirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}