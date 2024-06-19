namespace Unity.Services.Core.Editor.Settings
{
    class EnvironmentSettings
    {
        public string EnvironmentName;
        public EnvironmentSettings() {}
        public EnvironmentSettings(string environmentName)
        {
            EnvironmentName = environmentName;
        }
    }
}
