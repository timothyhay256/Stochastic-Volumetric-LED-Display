using System.Collections.Generic;
using Unity.Services.Core.Analytics.Internal;
using Unity.Services.Core.Configuration.Internal;

namespace Unity.Services.Analytics.Internal
{
    internal class StandardEventServiceComponent : IAnalyticsStandardEventComponent
    {
        readonly IProjectConfiguration m_Configuration;
        readonly IUnstructuredEventRecorder m_AnalyticsService;

        public StandardEventServiceComponent(IProjectConfiguration configuration, IUnstructuredEventRecorder analyticsService)
        {
            m_Configuration = configuration;
            m_AnalyticsService = analyticsService;
        }

        public void Record(string eventName, IDictionary<string, object> eventParameters, int eventVersion, string packageName)
        {
            string packageVersion = m_Configuration.GetString($"{packageName}.version");
            string callerIdentifier = $"{packageName}@{packageVersion}";

            m_AnalyticsService.CustomData(eventName, eventParameters, eventVersion, true, true, callerIdentifier);
        }
    }
}
