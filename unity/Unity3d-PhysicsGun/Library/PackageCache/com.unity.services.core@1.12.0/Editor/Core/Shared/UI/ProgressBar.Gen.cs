// WARNING: Auto generated code. Modifications will be lost!
using UnityEditor;

namespace Unity.Services.Core.Editor.Shared.UI
{
    sealed class ProgressBar : IProgressBar
    {
        readonly string m_ProgressBarTitle;
        readonly int m_NbTotalSteps;
        int m_NbCompletedSteps;

        public string OperationInfo { get; set; }

        public ProgressBar(string title, string operationInfo, int totalSteps)
        {
            m_ProgressBarTitle = title;
            OperationInfo = operationInfo;
            m_NbTotalSteps = totalSteps;
            UpdateProgressBar();
            AssemblyReloadEvents.beforeAssemblyReload += EditorUtility.ClearProgressBar;
        }

        public void CompleteStep()
        {
            m_NbCompletedSteps++;
            UpdateProgressBar();
        }

        void UpdateProgressBar()
        {
            var info = $"{OperationInfo} ({m_NbCompletedSteps}/{m_NbTotalSteps})";
            EditorUtility.DisplayProgressBar(m_ProgressBarTitle,
                info,
                (float)m_NbCompletedSteps / m_NbTotalSteps);
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
            AssemblyReloadEvents.beforeAssemblyReload -= EditorUtility.ClearProgressBar;
        }
    }
}
