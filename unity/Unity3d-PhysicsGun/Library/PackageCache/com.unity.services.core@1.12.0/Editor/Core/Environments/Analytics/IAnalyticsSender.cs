using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    interface IAnalyticsSender
    {
        AnalyticsResult SendEvent(object parameters);
    }
}
