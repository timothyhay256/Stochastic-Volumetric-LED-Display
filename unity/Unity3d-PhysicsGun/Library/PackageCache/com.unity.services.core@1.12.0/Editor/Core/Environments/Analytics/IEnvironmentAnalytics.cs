using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    interface IEnvironmentAnalytics
    {
        AnalyticsResult SendEnvironmentChangedEvent(string environmentGuid);
    }
}
