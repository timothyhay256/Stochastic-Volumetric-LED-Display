using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Unity.Services.Core.Internal.Serialization
{
    class NewtonsoftSerializer : IJsonSerializer
    {
        readonly JsonSerializer m_Serializer;

        public NewtonsoftSerializer(JsonSerializerSettings settings = null)
            : this(JsonSerializer.Create(settings)) {}

        internal NewtonsoftSerializer(JsonSerializer serializer)
            => m_Serializer = serializer;

        public string SerializeObject<T>(T value)
        {
            var builder = new StringBuilder(256);
            using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = m_Serializer.Formatting;
                m_Serializer.Serialize(jsonWriter, value, typeof(T));

                return writer.ToString();
            }
        }

        public T DeserializeObject<T>(string value)
        {
            using (var reader = new JsonTextReader(new StringReader(value)))
            {
                return (T)m_Serializer.Deserialize(reader, typeof(T));
            }
        }
    }
}
