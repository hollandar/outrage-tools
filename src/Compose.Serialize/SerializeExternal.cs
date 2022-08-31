namespace Compose.Serialize
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SerializeExternal:Attribute
    {
        public SerializeExternal(bool asCollection = false)
        {
            this.AsCollection = asCollection;
        }

        public bool AsCollection { get; set; } = false;
    }
}