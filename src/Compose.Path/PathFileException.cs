using System.Runtime.Serialization;

namespace Compose.Path
{
    [Serializable]
    internal class PathFileException : Exception
    {
        public PathFileException()
        {
        }

        public PathFileException(string? message) : base(message)
        {
        }

        public PathFileException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected PathFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}