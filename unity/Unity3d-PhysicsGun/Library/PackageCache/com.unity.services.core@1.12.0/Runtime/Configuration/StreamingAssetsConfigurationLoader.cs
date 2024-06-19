using System.Threading.Tasks;
using Unity.Services.Core.Internal.Serialization;

namespace Unity.Services.Core.Configuration
{
    class StreamingAssetsConfigurationLoader : IConfigurationLoader
    {
        readonly IJsonSerializer m_Serializer;
        public StreamingAssetsConfigurationLoader(IJsonSerializer serializer) => m_Serializer = serializer;

        public async Task<SerializableProjectConfiguration> GetConfigAsync()
        {
            var jsonConfig = await StreamingAssetsUtils.GetFileTextFromStreamingAssetsAsync(
                ConfigurationUtils.ConfigFileName);
            var config = m_Serializer.DeserializeObject<SerializableProjectConfiguration>(jsonConfig);
            return config;
        }
    }
}
