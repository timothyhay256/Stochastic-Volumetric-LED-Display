using UnityEditor;

namespace Unity.Services.Analytics.Editor
{
    [InitializeOnLoad]
    class CoreStatsIntegration
    {
        static CoreStatsIntegration()
        {
#if ENABLE_UNITY_GAME_SERVICES_ANALYTICS_SUPPORT
            UnityEditor.Analytics.AnalyticsSettings.OnRequireInBuildHandler += () =>
            {
                return true;
            };
#endif
        }
    }
}
