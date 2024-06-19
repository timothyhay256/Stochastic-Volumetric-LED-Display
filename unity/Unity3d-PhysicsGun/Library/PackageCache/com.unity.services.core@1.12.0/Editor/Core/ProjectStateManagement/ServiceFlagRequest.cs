using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine.Networking;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Internal.Serialization;

namespace Unity.Services.Core.Editor
{
    class ServiceFlagRequest : IServiceFlagRequest
    {
        const string k_ServiceFlagsKey = "service_flags";

        readonly IJsonSerializer m_Serializer;

        public ServiceFlagRequest(IJsonSerializer serializer) => m_Serializer = serializer;

        public IAsyncOperation<IDictionary<string, bool>> FetchServiceFlags()
        {
            var resultAsyncOp = new AsyncOperation<IDictionary<string, bool>>();
            try
            {
                resultAsyncOp.SetInProgress();
                var cdnEndpoint = new DefaultCdnConfiguredEndpoint();
                var configurationRequestTask = cdnEndpoint.GetConfiguration();
                configurationRequestTask.Completed += configOperation => QueryProjectFlags(configOperation, resultAsyncOp);
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }

            return resultAsyncOp;
        }

        void QueryProjectFlags(
            IAsyncOperation<DefaultCdnEndpointConfiguration> configurationRequestTask,
            AsyncOperation<IDictionary<string, bool>> resultAsyncOp)
        {
            try
            {
#if ENABLE_EDITOR_GAME_SERVICES
                var organizationKey = CloudProjectSettings.organizationKey;
#else
                var organizationKey = CloudProjectSettings.organizationId;
#endif
                var projectApiUrl = configurationRequestTask.Result.BuildProjectApiUrl(organizationKey, CloudProjectSettings.projectId);
                var getProjectFlagsRequest = new UnityWebRequest(
                    projectApiUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getProjectFlagsRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {CloudProjectSettings.accessToken}");
                var operation = getProjectFlagsRequest.SendWebRequest();
                operation.completed += op => OnFetchServiceFlagsCompleted(getProjectFlagsRequest, resultAsyncOp);
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }
        }

        void OnFetchServiceFlagsCompleted(UnityWebRequest getServiceFlagsRequest, AsyncOperation<IDictionary<string, bool>> resultAsyncOp)
        {
            try
            {
                resultAsyncOp.Succeed(ExtractServiceFlagsFromUnityWebRequest(getServiceFlagsRequest));
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }
            finally
            {
                getServiceFlagsRequest.Dispose();
            }
        }

        IDictionary<string, bool> ExtractServiceFlagsFromUnityWebRequest(UnityWebRequest unityWebRequest)
        {
            if (!UnityWebRequestHelper.IsUnityWebRequestReadyForTextExtract(unityWebRequest, out var jsonContent))
            {
                return null;
            }

            IDictionary<string, bool> flags;
            try
            {
                var jsonEntries = m_Serializer.DeserializeObject<JObject>(jsonContent);
                flags = ((JObject)jsonEntries ? [k_ServiceFlagsKey])?.ToObject<IDictionary<string, bool>>();
            }
            catch (Exception ex)
            {
                CoreLogger.LogError($"Exception occurred when fetching service flags:\n{ex}");
                flags = new Dictionary<string, bool>();
            }

            return flags;
        }
    }
}
