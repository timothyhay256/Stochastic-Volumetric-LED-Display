#if UNITY_2023_2_OR_NEWER
using System;
using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    [AnalyticInfo(
        eventName: EditorGameServiceAnalyticData.EventName,
        vendorKey: EditorGameServiceAnalyticsSender.VendorKey,
        version: EditorGameServiceAnalyticData.Version)]
    class EnvironmentAnalyticEvent : IAnalytic
    {
        EnvironmentChangedParameters m_EnvironmentChangedParameters;

        public EnvironmentAnalyticEvent(EnvironmentChangedParameters environmentChangedParameters)
        {
            m_EnvironmentChangedParameters = environmentChangedParameters;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_EnvironmentChangedParameters;
            error = null;
            return data != null;
        }
    }
}
#endif
