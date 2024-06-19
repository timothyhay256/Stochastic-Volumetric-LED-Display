using System.IO;
using Unity.Services.Core.Internal.Serialization;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Core.Configuration.Editor
{
    static class ProjectConfigurationBuilderHelper
    {
        public static string RuntimeConfigFullPath { get; }
            = Path.Combine(Application.streamingAssetsPath, ConfigurationUtils.ConfigFileName);

        public static void GenerateConfigFileInProject(
            ProjectConfigurationBuilder builder, IJsonSerializer serializer)
        {
            var config = builder.BuildConfiguration();
            var serializedConfig = serializer.SerializeObject(config);
            AddConfigToProject(serializedConfig);
        }

        internal static void AddConfigToProject(string config)
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            File.WriteAllText(RuntimeConfigFullPath, config);
            AssetDatabase.Refresh();
        }
    }
}
