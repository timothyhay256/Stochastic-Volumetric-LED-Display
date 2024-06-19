// WARNING: Auto generated code. Modifications will be lost!
using System;
using UnityEditor;

namespace Unity.Services.Core.Editor.Shared.UI
{
    interface IProgressBar : IDisposable
    {
        string OperationInfo { get; set; }
        void CompleteStep();
    }

    interface INotifications
    {
        IProgressBar ProgressBar(string title, string operationInfo, int totalSteps);
        bool DisplayDialog(string title, string dialogContent, string ok);
    }

    class Notifications : INotifications
    {
        public IProgressBar ProgressBar(string title, string operationInfo, int totalSteps)
        {
            return new ProgressBar(title, operationInfo, totalSteps);
        }

        public bool DisplayDialog(string title, string dialogContent, string ok)
        {
            return EditorUtility.DisplayDialog(title, dialogContent, ok);
        }
    }
}
