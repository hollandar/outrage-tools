using Compose.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compose.Serialize
{
    public interface ICustomDeserializer
    {
        public object? Deserialize(PathBuilder path, SerializerOptions serializerOptions);
    }
}
