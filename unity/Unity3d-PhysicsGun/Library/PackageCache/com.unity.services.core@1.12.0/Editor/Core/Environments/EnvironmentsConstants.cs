using UnityEditor;

namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// The class where constants related to environments are stored.
    /// </summary>
    static class EnvironmentsConstants
    {
        internal const string ServiceName = "Environments";
        static readonly string k_SettingsLocation = $"Project/Services/{ServiceName}";

        /// <summary>
        /// The location in Project Settings for Environments.
        /// </summary>
        public static readonly string SettingsLocation = k_SettingsLocation;
    }
}
