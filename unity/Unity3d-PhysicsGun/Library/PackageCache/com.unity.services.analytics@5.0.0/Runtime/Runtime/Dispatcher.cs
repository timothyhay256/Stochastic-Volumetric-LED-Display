using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.Analytics.Internal
{
    interface IDispatcher
    {
        int ConsecutiveFailedUploadCount { get; }

        void SetBuffer(IBuffer buffer);

        void Flush();
    }

    class Dispatcher : IDispatcher
    {
        readonly IWebRequestHelper m_WebRequestHelper;
        readonly string m_CollectUrl;

        internal const string k_PiplConsentHeaderKey = "PIPL_CONSENT";
        internal const string k_PiplExportHeaderKey = "PIPL_EXPORT";
        internal const string k_HeaderTrueValue = "true";

        IBuffer m_DataBuffer;
        IWebRequest m_FlushRequest;

        public int ConsecutiveFailedUploadCount { get; private set; }

        internal bool FlushInProgress { get; private set; }

        private int m_FlushBufferIndex;

        public Dispatcher(IWebRequestHelper webRequestHelper, string collectUrl)
        {
            m_WebRequestHelper = webRequestHelper;
            m_CollectUrl = collectUrl;
        }

        public void SetBuffer(IBuffer buffer)
        {
            m_DataBuffer = buffer;
        }

        public void Flush()
        {
            if (FlushInProgress)
            {
                Debug.LogWarning("Analytics Dispatcher is already flushing.");
            }
            else
            {
                FlushBufferToService();
            }
        }

        void FlushBufferToService()
        {
            FlushInProgress = true;

            var postBytes = m_DataBuffer.Serialize();
            m_FlushBufferIndex = m_DataBuffer.Length;

            if (postBytes == null || postBytes.Length == 0)
            {
                FlushInProgress = false;
                m_FlushBufferIndex = 0;
            }
            else
            {
                m_FlushRequest = m_WebRequestHelper.CreateWebRequest(m_CollectUrl, UnityWebRequest.kHttpVerbPOST, postBytes);

                m_FlushRequest.SetRequestHeader(k_PiplExportHeaderKey, k_HeaderTrueValue);
                m_FlushRequest.SetRequestHeader(k_PiplConsentHeaderKey, k_HeaderTrueValue);

                m_WebRequestHelper.SendWebRequest(m_FlushRequest, UploadCompleted);

#if UNITY_ANALYTICS_EVENT_LOGS
                Debug.Log("Uploading events...");
#endif
            }
        }

        void UploadCompleted(long responseCode)
        {
            bool success = responseCode >= 200 && responseCode <= 299;
            bool badRequest = responseCode >= 400 && responseCode <= 499;
            bool intermittentError = responseCode >= 500 && responseCode <= 599 || m_FlushRequest.IsNetworkError;

            if (success || badRequest)
            {
#if UNITY_ANALYTICS_EVENT_LOGS
                if (ConsecutiveFailedUploadCount > 0)
                {
                    Debug.Log("An upload request finally got through, consecutive failure count has been reset.");
                }
#endif

                ConsecutiveFailedUploadCount = 0;

                // If we get a 2xx response code, the request is good and has gone through successfully,
                // so we can safely discard the buffer knowing the data has reached its destination.
                // If we get a 4xx response code, the request is bad and we should discard the buffer.
                // Reasons include:
                //  - JSON is malformed; we have a bad event somewhere and all we can do is bin the lot
                //  - Project has been usage-gated and turned off; we will never get data through so might as well drop it all

                m_DataBuffer.ClearBuffer(m_FlushBufferIndex);
                m_DataBuffer.ClearDiskCache();

#if UNITY_ANALYTICS_EVENT_LOGS
                if (success)
                {
                    Debug.Log("Events uploaded successfully!");
                }
                else
                {
                    Debug.Log($"Events upload failed ({responseCode}). Please visit the UGS Analytics dashboard to check for any relevant alerts. The event buffer has been cleared.");
                }
#endif
            }
            else if (intermittentError)
            {
                ConsecutiveFailedUploadCount++;

                // Flush to disk in case we end up exiting before connectivity is re-established.
                m_DataBuffer.FlushToDisk();

#if UNITY_ANALYTICS_EVENT_LOGS
                if (m_FlushRequest.IsNetworkError)
                {
                    Debug.Log("Events failed to upload (network error) -- upload will be reattempted later.");
                }
                else
                {
                    Debug.LogFormat("Events failed to upload (code {0}) -- upload will be reattempted later.", responseCode);
                }
#endif
            }

            // Clear the request now that we are done.
            FlushInProgress = false;
            m_FlushBufferIndex = 0;
            m_FlushRequest.Dispose();
            m_FlushRequest = null;
        }
    }
}
