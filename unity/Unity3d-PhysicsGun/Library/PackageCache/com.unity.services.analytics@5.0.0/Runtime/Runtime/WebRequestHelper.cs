using System;
using UnityEngine.Networking;

namespace Unity.Services.Analytics.Internal
{
    interface IWebRequest
    {
        UnityWebRequestAsyncOperation SendWebRequest();
        UploadHandler uploadHandler { get; set; }
        void SetRequestHeader(string key, string value);
        bool IsNetworkError { get; }
        void Dispose();
    }

    interface IWebRequestHelper
    {
        IWebRequest CreateWebRequest(string url, string method, byte[] postBytes);
        void SendWebRequest(IWebRequest request, Action<long> onCompleted);
    }

    class AnalyticsWebRequest : UnityWebRequest, IWebRequest
    {
        internal AnalyticsWebRequest(string url, string method) : base(url, method) {}

        public bool IsNetworkError
        {
            get
            {
#if UNITY_2020_1_OR_NEWER
                return result == UnityWebRequest.Result.ConnectionError;
#else
                return isNetworkError;
#endif
            }
        }
    }

    class WebRequestHelper : IWebRequestHelper
    {
        readonly string k_ClientIdHeaderValue = "com.unity.services.analytics@" + SdkVersion.SDK_VERSION;

        public IWebRequest CreateWebRequest(string url, string method, byte[] postBytes)
        {
            var request = new AnalyticsWebRequest(url, method);
            var upload = new UploadHandlerRaw(postBytes)
            {
                contentType = "application/json"
            };
            request.uploadHandler = upload;
            request.SetRequestHeader("x-client-id", k_ClientIdHeaderValue);
            return request;
        }

        public void SendWebRequest(IWebRequest request, Action<long> onCompleted)
        {
            var requestOp = request.SendWebRequest();
            requestOp.completed += delegate
            {
                onCompleted(requestOp.webRequest.responseCode);
            };
        }
    }
}
