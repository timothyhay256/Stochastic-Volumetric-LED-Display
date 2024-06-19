using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    class EnvironmentAnalytics : IEnvironmentAnalytics
    {
        const string k_ActionChangeEnvironment = "environment_changed";
        readonly IAnalyticsSender m_AnalyticsSender;

        public EnvironmentAnalytics(IAnalyticsSender analyticsSender)
        {
            m_AnalyticsSender = analyticsSender;
        }

        public AnalyticsResult SendEnvironmentChangedEvent(string environmentGuid)
        {
            return m_AnalyticsSender.SendEvent(new EnvironmentChangedParameters()
            {
                action = k_ActionChangeEnvironment,
                component = environmentGuid,
                package = "Core.Environments",
            });
        }
    }
}
