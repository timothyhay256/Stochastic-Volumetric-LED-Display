using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        public void TransactionFailed(TransactionFailedParameters parameters)
        {
            if (m_IsActive)
            {
                if (String.IsNullOrEmpty(parameters.TransactionName))
                {
                    Debug.LogError("Required to have a value for transactionName");
                }

                if (parameters.TransactionType.Equals(TransactionType.INVALID))
                {
                    Debug.LogError("Required to have a value for transactionType");
                }

                if (String.IsNullOrEmpty(parameters.FailureReason))
                {
                    Debug.LogError("Required to have a failure reason in transactionFailed event");
                }

                if (String.IsNullOrEmpty(parameters.PaymentCountry))
                {
                    parameters.PaymentCountry = Internal.Platform.UserCountry.Name();
                }

                m_DataGenerator.TransactionFailed(DateTime.Now, m_CommonParams, "com.unity.services.analytics.events.TransactionFailed", parameters);
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            else
            {
                Debug.Log("Did not record transactionFailed event because player has not opted in.");
            }
#endif
        }
    }
}
