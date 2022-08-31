namespace Compose.Serialize
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SerializeContainer:Attribute
    {
        public SerializeContainer(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}