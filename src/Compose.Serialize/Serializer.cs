using Compose.Path;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Compose.Serialize
{
    public enum SerializationFormatEnum { json, yaml, folderYaml, folderJson }
    public class SerializerOptions
    {
        public SerializationFormatEnum SerializationFormat { get; set; } = SerializationFormatEnum.json;
        public string ObjectName { get; set; } = "object";

        public Func<JsonSerializer>? CreateJsonSerializer { get; set; } = null;
        public Func<IDeserializer>? CreateYamlDeserializer { get; set; } = null;
        public Func<ISerializer>? CreateYamlSerializer { get; set; } = null;
        public IDictionary<string, ICustomDeserializer> Deserializers { get; } = new Dictionary<string, ICustomDeserializer>();

        public void AddDeserializer<TType>(string extension) where TType: ICustomDeserializer, new()
        {
            this.Deserializers[extension] = new TType();
        }
    }
    public static class Serializer
    {
        static string[] extensions = new string[] { "", ".json", ".yaml" };
        public static TType? DeserializeExt<TType>(PathBuilder path, SerializerOptions? options = null) where TType : new()
        {
            foreach (var extension in extensions)
            {
                var extFile = path.WithExtension(extension);
                if (extFile.IsFile)
                {
                    return Deserialize<TType>(extFile, options);
                }
            }

            throw new FileNotFoundException($"{path} does not exist, extensions were also tried.");
        }

        public static TType? Deserialize<TType>(PathBuilder path, SerializerOptions? options = null) where TType : new()
        {
            return (TType?)Deserialize(typeof(TType?), path, options);
        }

        public static object? Deserialize(Type type, PathBuilder path, SerializerOptions? options = null)
        {
            var serializerOptions = options ?? new();
            if (path.IsFile)
            {
                try
                {
                    if (path.Extension == ".json")
                    {
                        var jsonDeserializer = serializerOptions.CreateJsonSerializer == null ? new JsonSerializer() : serializerOptions.CreateJsonSerializer();

                        using var reader = new StringReader(path.ReadToEnd());
                        return jsonDeserializer.Deserialize(reader, type);
                    }
                    else if (path.IsFile && path.Extension == ".yaml")
                    {
                        var yamlDeserializer = serializerOptions.CreateYamlDeserializer == null ? new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build() : serializerOptions.CreateYamlDeserializer();

                        return yamlDeserializer.Deserialize(path.ReadToEnd(), type);
                    }
                    else
                    {
                        foreach (var deserializer in serializerOptions.Deserializers)
                        {
                            if (path.IsFile && path.Extension == deserializer.Key)
                            {
                                return deserializer.Value.Deserialize(path, serializerOptions);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new SerializationException($"Could not deserialize {path} to {type}.", e);
                }
            }

            if (path.IsDirectory)
            {
                var instanceConstructor = type.GetConstructor(Array.Empty<Type>());
                if (instanceConstructor == null)
                    throw new ArgumentException($"{type.ToString()} does not have a default constructor.");

                object? deserializedObject = instanceConstructor.Invoke(null);

                var customAttributes = type.GetCustomAttributes(typeof(SerializeContainer), true).Cast<SerializeContainer>();
                var container = customAttributes.FirstOrDefault();
                var objectName = serializerOptions.ObjectName;
                if (container != null)
                {
                    objectName = container.Name;
                }

                var propertyNames = type.GetProperties().Where(r => r.GetCustomAttributes(typeof(SerializeExternal), true).Any()).Select(r => r.Name.ToLower()).ToHashSet();
                if (propertyNames.Contains(objectName.ToLower()))
                {
                    throw new ObjectNameCollisionException($"Object name {objectName} is the same name as a serialize external property, this configuration is not supported.  Use the SerializeContainer(name) attribute to change the serialization name for the object {type.FullName}.");
                }

                var fileNameJson = path / $"{objectName}.json";
                if (fileNameJson.IsFile)
                {
                    deserializedObject = Deserialize(type, fileNameJson, serializerOptions);
                    if (deserializedObject == null)
                        throw new SerializationException($"{fileNameJson} deserialized to a null value.");
                }

                var fileNameYaml = path / $"{objectName}.yaml";
                if (fileNameYaml.IsFile)
                {
                    deserializedObject = Deserialize(type, fileNameYaml, serializerOptions);
                    if (deserializedObject == null)
                        throw new SerializationException($"{fileNameYaml} deserialized to a null value.");
                }

                foreach (var deserializer in serializerOptions.Deserializers)
                {
                    var deserializerFilename = path / $"{objectName}{deserializer.Key}";
                    if (deserializerFilename.IsFile)
                    {
                        deserializedObject = Deserialize(type, deserializerFilename, serializerOptions);
                        if (deserializedObject == null)
                            throw new SerializationException($"{deserializerFilename} deserialized to a null value.");
                    }
                }

                var serializerType = deserializedObject.GetType();
                var setterProperties = serializerType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var externalProperty in setterProperties)
                {
                    var serializeExternal = (SerializeExternal?)externalProperty.GetCustomAttributes(typeof(SerializeExternal), true).FirstOrDefault();
                    if (serializeExternal == null || !externalProperty.CanWrite) continue;

                    var folderPath = path / externalProperty.Name;
                    if (serializeExternal.AsCollection || folderPath.IsDirectory)
                    {
                        var internalType = externalProperty.PropertyType.GetGenericArguments().First();
                        var collectionType = typeof(IEnumerable<>).MakeGenericType(internalType);
                        var constructor = typeof(List<>).MakeGenericType(internalType).GetConstructor(new Type[] { collectionType });
                        var currentValue = externalProperty.GetValue(deserializedObject);
                        var list = constructor!.Invoke(new object?[] { currentValue });
                        var listAdd = (object item) => list.GetType().GetMethod("Add")!.Invoke(list, new object[] { item });
                        var items = folderPath.ListFiles();
                        foreach (var item in items)
                        {
                            if (item.IsFile && item.FilenameWithoutExtension != objectName && (item.Extension == ".json" || item.Extension == ".yaml" || serializerOptions.Deserializers.Keys.Contains(item.Extension)))
                            {
                                var value = Deserialize(internalType, item, serializerOptions);
                                if (value != null) listAdd(value);
                            }
                        }

                        try
                        {
                            externalProperty.SetValue(deserializedObject, list);
                        }
                        catch (Exception e)
                        {
                            throw new ListIncompatibleException($"The field {externalProperty.Name} is stored as a folder, but is not compatible with a IList.  It should be ICollection<>, IEnumerable<>, IList<> or List<>.", e);
                        }
                    }
                    else
                    {
                        var jsonPath = path / $"{externalProperty.Name}.json";
                        if (jsonPath.IsFile)
                        {
                            try
                            {
                                var value = Deserialize(externalProperty.PropertyType, jsonPath, serializerOptions);
                                externalProperty.SetValue(deserializedObject, value);
                            }
                            catch (Exception e)
                            {
                                throw new SerializationException($"Could not deserialize {jsonPath} to {externalProperty.PropertyType}.", e);
                            }
                        }
                        var yamlPath = path / $"{externalProperty.Name}.yaml";
                        if (yamlPath.IsFile)
                        {
                            try
                            {
                                var value = Deserialize(externalProperty.PropertyType, yamlPath, serializerOptions);
                                externalProperty.SetValue(deserializedObject, value);
                            }
                            catch (Exception e)
                            {
                                throw new SerializationException($"Could not deserialize {yamlPath} to {externalProperty.PropertyType}.", e);
                            }
                        }
                        foreach (var deserializer in serializerOptions.Deserializers)
                        {
                            var deserializerFilename = path / $"{objectName}{deserializer.Key}";
                            if (deserializerFilename.IsFile)
                            {
                                try
                                {
                                    var value = Deserialize(type, deserializerFilename, serializerOptions);
                                    externalProperty.SetValue(deserializedObject, value);
                                }
                                catch (Exception e)
                                {
                                    throw new SerializationException($"Could not deserialize {deserializerFilename} to {externalProperty.PropertyType}.", e);
                                }
                            }
                        }
                    }
                }
                return deserializedObject;
            }

            throw new SerializationException($"Could not deserialize {type.ToString()} from {path.ToString()}.");
        }

        public static string Serialize<TType>(TType serializableObject, SerializerOptions? options = null)
        {
            return Serialize(serializableObject, options);
        }

        public static string Serialize(object serializableObject, SerializerOptions? options = null)
        {
            var serializerOptions = options ?? new();
            switch (serializerOptions.SerializationFormat)
            {
                case SerializationFormatEnum.json:
                    return JsonConvert.SerializeObject(serializableObject);
                case SerializationFormatEnum.yaml:
                    {
                        var serializer = new SerializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .Build();

                        return serializer.Serialize(serializableObject);
                    }
                default:
                    throw new NotImplementedException($"Dont know how to process the {serializerOptions.SerializationFormat.ToString()} format.");
            }
        }


    }
}
