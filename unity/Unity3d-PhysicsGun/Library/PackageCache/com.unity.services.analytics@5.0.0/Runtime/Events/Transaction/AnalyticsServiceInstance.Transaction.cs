using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        readonly TransactionCurrencyConverter converter = new TransactionCurrencyConverter();

        public void Transaction(TransactionParameters transactionParameters)
        {
            if (m_IsActive)
            {
                if (String.IsNullOrEmpty(transactionParameters.TransactionName))
                {
                    Debug.LogError("Required to have a value for transactionName");
                }

                if (transactionParameters.TransactionType.Equals(TransactionType.INVALID))
                {
                    Debug.LogError("Required to have a value for transactionType");
                }

                // If the paymentCountry is not provided we will generate it.
                if (String.IsNullOrEmpty(transactionParameters.PaymentCountry))
                {
                    transactionParameters.PaymentCountry = Internal.Platform.UserCountry.Name();
                }

                m_DataGenerator.Transaction(DateTime.Now, m_CommonParams, "com.unity.services.analytics.events.transaction", transactionParameters);
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            else
            {
                Debug.Log("Did not record transaction event because player has not opted in.");
            }
#endif
        }

        public long ConvertCurrencyToMinorUnits(string currencyCode, double value)
        {
            return converter.Convert(currencyCode, value);
        }
    }
}
























































































































































































































































