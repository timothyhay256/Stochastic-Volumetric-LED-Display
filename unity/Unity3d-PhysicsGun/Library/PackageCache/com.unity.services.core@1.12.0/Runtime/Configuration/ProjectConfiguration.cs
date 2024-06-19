using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Internal.Serialization;

namespace Unity.Services.Core.Configuration
{
    class ProjectConfiguration : IProjectConfiguration
    {
        string m_JsonCache;
        readonly IReadOnlyDictionary<string, ConfigurationEntry> m_ConfigValues;
        internal IJsonSerializer Serializer { get; }

        public ProjectConfiguration(
            IReadOnlyDictionary<string, ConfigurationEntry> configValues, IJsonSerializer serializer)
        {
            m_ConfigValues = configValues;
            Serializer = serializer;
        }

        public bool GetBool(string key, bool defaultValue = default)
        {
            var stringConfig = GetString(key);
            return bool.TryParse(stringConfig, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        public int GetInt(string key, int defaultValue = default)
        {
            var stringConfig = GetString(key);
            return int.TryParse(stringConfig, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = default)
        {
            var stringConfig = GetString(key);
            return float.TryParse(stringConfig, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : defaultValue;
        }

        public string GetString(string key, string defaultValue = default)
        {
            return m_ConfigValues.TryGetValue(key, out var configValue)
                ? configValue.Value
                : defaultValue;
        }

        public string ToJson()
        {
            if (m_JsonCache == null)
            {
                var dict = m_ConfigValues.ToDictionary(pair => pair.Key, pair => pair.Value.Value);
                m_JsonCache = Serializer.SerializeObject(dict);
            }

            return m_JsonCache;
        }
    }
}
