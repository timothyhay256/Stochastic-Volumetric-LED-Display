using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.Analytics.Internal
{
    interface IAnalyticsForgetter
    {
        bool DeletionInProgress { get; }
        void ResetDataDeletionStatus();
        void AttemptToForget(string userId, string installationId, string playerId, string timestamp, string callingMethod, Action successfulUploadCallback);
    }

    class AnalyticsForgetter : IAnalyticsForgetter
    {
        const string k_ForgottenStatusKey = "unity.services.analytics.data_deletion_status";

        enum DataDeletionStatus
        {
            DataAllowed,
            DeletionInProgress,
            SuccessfullyDeleted
        }

        readonly string m_CollectUrl;
        readonly IPersistence m_Persistence;
        readonly IWebRequestHelper m_WebRequestHelper;

        byte[] m_Event;
        Action m_Callback;

        DataDeletionStatus m_DeletionStatus;
        IWebRequest m_Request;

        public bool DeletionInProgress
        {
            get { return m_DeletionStatus == DataDeletionStatus.DeletionInProgress; }
        }

        internal AnalyticsForgetter(string collectUrl, IPersistence persistence, IWebRequestHelper webRequestHelper)
        {
            m_CollectUrl = collectUrl;
            m_Persistence = persistence;
            m_WebRequestHelper = webRequestHelper;

            m_DeletionStatus = (DataDeletionStatus)persistence.LoadValue(k_ForgottenStatusKey);
        }

        public void ResetDataDeletionStatus()
        {
            SetForgettingStatus(DataDeletionStatus.DataAllowed);
        }

        void SetForgettingStatus(DataDeletionStatus state)
        {
            m_DeletionStatus = state;
            m_Persistence.SaveValue(k_ForgottenStatusKey, (int)state);
        }

        public void AttemptToForget(string userId, string installationId, string playerId, string timestamp, string callingMethod, Action successfulUploadCallback)
        {
            if (m_Request == null)
            {
#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Sending data deletion request...");
#endif
                SetForgettingStatus(DataDeletionStatus.DeletionInProgress);

                m_Callback = successfulUploadCallback;

                // NOTE: we cannot use String.Format on JSON because it gets confused by all the {}s
                var eventJson =
                    "{\"eventList\":[{" +
                    "\"eventName\":\"ddnaForgetMe\"," +
                    "\"userID\":\"" + userId + "\"," +
                    "\"eventUUID\":\"" + Guid.NewGuid().ToString() + "\"," +
                    "\"eventTimestamp\":\"" + timestamp + "\"," +
                    "\"eventVersion\":1," +
                    "\"unityInstallationID\":\"" + installationId + "\"," +
                    (String.IsNullOrEmpty(playerId) ? "" : "\"unityPlayerID\":\"" + playerId + "\",") +
                    "\"eventParams\":{" +
                    "\"clientVersion\":\"" + Application.version + "\"," +
                    "\"sdkMethod\":\"" + callingMethod + "\"" +
                    "}}]}";

                m_Event = Encoding.UTF8.GetBytes(eventJson);

                m_Request = m_WebRequestHelper.CreateWebRequest(m_CollectUrl, UnityWebRequest.kHttpVerbPOST, m_Event);

                m_Request.SetRequestHeader(Dispatcher.k_PiplExportHeaderKey, Dispatcher.k_HeaderTrueValue);
                m_Request.SetRequestHeader(Dispatcher.k_PiplConsentHeaderKey, Dispatcher.k_HeaderTrueValue);

                m_WebRequestHelper.SendWebRequest(m_Request, UploadComplete);
            }
#if UNITY_ANALYTICS_DEVELOPMENT
            else
            {
                Debug.Log("Data deletion has already been successfully requested or completed. No need to upload the event again.");
            }
#endif
        }

        void UploadComplete(long code)
        {
            bool success = code >= 200 && code <= 299;

            if (!m_Request.IsNetworkError && success)
            {
#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Data deletion request successfully uploaded!");
#endif
                SetForgettingStatus(DataDeletionStatus.SuccessfullyDeleted);
                m_Callback();
            }
#if UNITY_ANALYTICS_DEVELOPMENT
            else
            {
                Debug.Log("Data deletion request failed to upload.");
            }
#endif

            // Clear the request to allow another request to be sent.
            m_Request.Dispose();
            m_Request = null;
        }
    }
}
