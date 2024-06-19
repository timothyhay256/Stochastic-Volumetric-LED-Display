using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        /// <summary>
        /// Record an Ad Impression event.
        /// </summary>
        /// <param name="adImpressionParameters">(Required) Helper object to handle arguments.</param>
        public void AdImpression(AdImpressionParameters adImpressionParameters)
        {
            if (m_IsActive)
            {
                if (String.IsNullOrEmpty(adImpressionParameters.PlacementID))
                {
                    Debug.LogError("Required to have a value for placementID.");
                }

                if (String.IsNullOrEmpty(adImpressionParameters.PlacementName))
                {
                    Debug.LogError("Required to have a value for placementName.");
                }

                m_DataGenerator.AdImpression(DateTime.Now, m_CommonParams, "com.unity.services.analytics.events.adimpression", adImpressionParameters);
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            else
            {
                Debug.Log("Did not record adImpression event because player has not opted in.");
            }
#endif
        }
    }
}
