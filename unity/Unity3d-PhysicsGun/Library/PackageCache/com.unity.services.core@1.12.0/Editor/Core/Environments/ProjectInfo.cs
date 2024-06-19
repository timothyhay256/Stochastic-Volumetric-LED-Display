using UnityEditor;

namespace Unity.Services.Core.Editor.Environments
{
    class ProjectInfo : IProjectInfo
    {
        public string ProjectId => CloudProjectSettings.projectId;
        public string ProjectName => CloudProjectSettings.projectName;
    }
}
