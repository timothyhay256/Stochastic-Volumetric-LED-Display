#if UNITY_2023_2_OR_NEWER
using System;
using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor
{
    [AnalyticInfo(
        eventName: EditorGameServiceAnalyticData.EventName,
        vendorKey: EditorGameServiceAnalyticsSender.VendorKey,
        version: EditorGameServiceAnalyticData.Version)]
    class EditorGameServiceAnalytic : IAnalytic
    {
        EditorGameServiceAnalyticData m_Data;

        public EditorGameServiceAnalytic(string component, string action, string package)
        {
            m_Data = new EditorGameServiceAnalyticData
            {
                component = component,
                action = action,
                package = package
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return data != null;
        }
    }
}
#endif
