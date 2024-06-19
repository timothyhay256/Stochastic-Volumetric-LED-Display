#if !UNITY_2021_3_OR_NEWER
using Unity.Services.Core.Internal.Serialization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.Services.Core.Configuration.Editor
{
    class ProjectConfigurationBuildInjectorWithReport : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public static void RemoveConfigFromProject()
        {
            IoUtils.TryDeleteAssetFile(ProjectConfigurationBuilderHelper.RuntimeConfigFullPath);
            IoUtils.TryDeleteStreamAssetsFolder();
        }

        int IOrderedCallback.callbackOrder { get; }

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            var builderWithAllProviders = ProjectConfigurationBuilder.CreateBuilderWithAllProvidersInProject();
            ProjectConfigurationBuilderHelper.GenerateConfigFileInProject(
                builderWithAllProviders, new NewtonsoftSerializer());

            EditorApplication.update += RemoveConfigFromProjectWhenBuildEnds;

            void RemoveConfigFromProjectWhenBuildEnds()
            {
                if (BuildPipeline.isBuildingPlayer)
                {
                    return;
                }

                EditorApplication.update -= RemoveConfigFromProjectWhenBuildEnds;
                RemoveConfigFromProject();
            }
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) => RemoveConfigFromProject();
    }
}
#endif
