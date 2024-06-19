using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        /// <summary>
        /// Record an acquisitionSource event.
        /// </summary>
        /// <param name="acquisitionSourceParameters">(Required) Helper object to handle parameters.</param>
        public void AcquisitionSource(AcquisitionSourceParameters acquisitionSourceParameters)
        {
            if (m_IsActive)
            {
                if (string.IsNullOrEmpty(acquisitionSourceParameters.Channel))
                {
                    Debug.LogError("Required to have a value for channel");
                }
                if (string.IsNullOrEmpty(acquisitionSourceParameters.CampaignId))
                {
                    Debug.LogError("Required to have a value for campaignId");
                }
                if (string.IsNullOrEmpty(acquisitionSourceParameters.CreativeId))
                {
                    Debug.LogError("Required to have a value for creativeId");
                }
                if (string.IsNullOrEmpty(acquisitionSourceParameters.CampaignName))
                {
                    Debug.LogError("Required to have a value for campaignName");
                }
                if (string.IsNullOrEmpty(acquisitionSourceParameters.Provider))
                {
                    Debug.LogError("Required to have a value for provider");
                }

                m_DataGenerator.AcquisitionSource(DateTime.Now, m_CommonParams,
                    "com.unity.services.analytics.events.acquisitionSource", acquisitionSourceParameters);
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            else
            {
                Debug.Log("Did not record acquisitionSource event because player has not opted in.");
            }
#endif
        }
    }
}
