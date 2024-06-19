#if UNITY_2021_3_OR_NEWER
using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using Unity.Services.Core.Internal.Serialization;
using UnityEditor.Build;
using UnityEngine;

namespace Unity.Services.Core.Configuration.Editor
{
    class ProjectConfigurationBuildInjectorWithPlayerProcessor : BuildPlayerProcessor
    {
        internal const string IoErrorMessage = "Service configuration file couldn't be created."
            + " Be sure you have read/write access to your project's Library folder.";

        internal static readonly string ConfigCachePath
            = Path.Combine(AssetUtils.CoreLibraryFolderPath, ConfigurationUtils.ConfigFileName);

        readonly IJsonSerializer m_Serializer;

        public ProjectConfigurationBuildInjectorWithPlayerProcessor()
            : this(new NewtonsoftSerializer()) { }

        public ProjectConfigurationBuildInjectorWithPlayerProcessor(IJsonSerializer serializer)
        {
            m_Serializer = serializer;
        }

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var config = ProjectConfigurationBuilder.CreateBuilderWithAllProvidersInProject()
                .BuildConfiguration();
            CreateProjectConfigFile(config);
            buildPlayerContext.AddAdditionalPathToStreamingAssets(ConfigCachePath);
        }

        internal void CreateProjectConfigFile(SerializableProjectConfiguration config)
        {
            try
            {
                if (!Directory.Exists(AssetUtils.CoreLibraryFolderPath))
                {
                    Directory.CreateDirectory(AssetUtils.CoreLibraryFolderPath);
                }

                var serializedConfig = m_Serializer.SerializeObject(config);
                File.WriteAllText(ConfigCachePath, serializedConfig);
            }
            catch (SecurityException e)
                when (e.PermissionType == typeof(FileIOPermission)
                      && FakePredicateLogError())
            {
                // Never reached to avoid stack unwind.
            }
            catch (UnauthorizedAccessException)
                when (FakePredicateLogError())
            {
                // Never reached to avoid stack unwind.
            }

            bool FakePredicateLogError()
            {
                Debug.LogError(IoErrorMessage);
                return false;
            }
        }
    }
}
#endif
