using Unity.Services.Core.Editor.Settings;
using Unity.Services.Core.Internal.Serialization;

namespace Unity.Services.Core.Editor.Environments.Save
{
    class EnvironmentSaveSystem : IEnvironmentSaveSystem
    {
        const string k_EnvironmentSettingsPath = "ProjectSettings/Packages/com.unity.services.core/Settings.json";

        readonly IFileSystem m_FileSystem;
        readonly IJsonSerializer m_JsonSerializer;

        EnvironmentSettings m_EnvironmentSettings;

        public EnvironmentSaveSystem(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem;
            m_JsonSerializer = new NewtonsoftSerializer();
        }

        public void SaveEnvironment(string environment)
        {
            var fileContent = m_FileSystem.GetOrCreateFileContent(k_EnvironmentSettingsPath);
            m_EnvironmentSettings = m_JsonSerializer.DeserializeObject<EnvironmentSettings>(fileContent)
                ?? new EnvironmentSettings();

            m_EnvironmentSettings.EnvironmentName = environment;

            fileContent = m_JsonSerializer.SerializeObject(m_EnvironmentSettings);

            m_FileSystem.SaveFile(k_EnvironmentSettingsPath, fileContent);
        }

        public string LoadEnvironment()
        {
            if (m_EnvironmentSettings != null
                && string.IsNullOrEmpty(m_EnvironmentSettings.EnvironmentName))
            {
                return string.Empty;
            }

            var fileContent = m_FileSystem.GetOrCreateFileContent(k_EnvironmentSettingsPath);
            m_EnvironmentSettings = m_JsonSerializer.DeserializeObject<EnvironmentSettings>(fileContent)
                ?? new EnvironmentSettings();

            return m_EnvironmentSettings.EnvironmentName;
        }
    }
}
