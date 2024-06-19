using System;
using UnityEditor;

namespace Unity.Services.Core.Editor
{
    class EditorGameServiceAnalyticsSender : IEditorGameServiceAnalyticsSender
    {
#if UNITY_2023_2_OR_NEWER
        internal const string VendorKey = "unity.services.core.editor";
#endif

        static class AnalyticsComponent
        {
            public const string ProjectSettings = "Project Settings";
            public const string ProjectBindPopup = "Project Bind Popup";
        }

        static class AnalyticsAction
        {
            public const string GoToDashboard = "Go to Dashboard";
            public const string OpenProjectSettings = "Open Project Settings";
            public const string CloseProjectBindPopup = "Close Project Bind Popup";
            public const string ProjectBindPopupDisplayed = "Project Bind Popup Displayed";
            public const string ClickedSignUpLink = "Clicked Signup Link";
        }

        public void SendProjectSettingsGoToDashboardEvent(string package)
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.GoToDashboard, package);
        }

        public void SendProjectBindPopupCloseActionEvent(string package)
        {
            SendEvent(AnalyticsComponent.ProjectBindPopup, AnalyticsAction.CloseProjectBindPopup, package);
        }

        public void SendClickedSignUpLinkActionEvent(string package)
        {
            SendEvent(AnalyticsComponent.ProjectBindPopup, AnalyticsAction.ClickedSignUpLink, package);
        }

        public void SendProjectBindPopupOpenProjectSettingsEvent(string package)
        {
            SendEvent(AnalyticsComponent.ProjectBindPopup, AnalyticsAction.OpenProjectSettings, package);
        }

        public void SendProjectBindPopupDisplayedEvent(string package)
        {
            SendEvent(AnalyticsComponent.ProjectBindPopup, AnalyticsAction.ProjectBindPopupDisplayed, package);
        }

        static void SendEvent(string component, string action, string package)
        {
#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new EditorGameServiceAnalytic(component, action, package));
#else
            EditorAnalytics.SendEventWithLimit(
                EditorGameServiceAnalyticData.EventName,
                new EditorGameServiceAnalyticData { action = action, component = component, package = package },
                EditorGameServiceAnalyticData.Version);
#endif
        }
    }
}
