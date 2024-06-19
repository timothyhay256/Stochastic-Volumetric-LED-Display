using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Shared.Clients;
using Unity.Services.Core.Environments.Client.Apis.Default;
using Unity.Services.Core.Environments.Client.Http;
using UnityEditor;

namespace Unity.Services.Core.Editor.Environments
{
    class EnvironmentsClientFactory : IEnvironmentFetcher
    {
        const string k_StagingUrl = "https://staging.services.unity.com";

        readonly IAccessTokens m_AccessTokens;

        public EnvironmentsClientFactory(IAccessTokens accessTokens)
        {
            m_AccessTokens = accessTokens;
        }

        public async Task<List<EnvironmentInfo>> FetchEnvironments()
        {
            var environmentsApi = await Build();

            if (environmentsApi == null)
            {
                return null;
            }

            return await environmentsApi.GetEnvironments();
        }

        public async Task<EnvironmentInfo?> FetchEnvironment(string environmentId)
        {
            var environmentsApi = await Build();

            if (environmentsApi == null)
            {
                return null;
            }

            return await environmentsApi.GetEnvironment(environmentId);
        }

        async Task<EnvironmentsClient> Build()
        {
            var projectId = CloudProjectSettings.projectId;
            var gatewayToken = await m_AccessTokens.GetServicesGatewayTokenAsync();

            if (gatewayToken == null)
            {
                return null;
            }

            string baseUrl = null;
            var env = new CloudEnvironmentConfigProvider();
            if (env.IsStaging())
            {
                baseUrl = k_StagingUrl;
            }

            var headers = new AdminApiHeaders<EnvironmentsClientFactory>(gatewayToken);
            var configuration = new Core.Environments.Client.Configuration(baseUrl, null, null, headers.ToDictionary());

            return new EnvironmentsClient(projectId, new DefaultApiClient(new HttpClient(), configuration));
        }
    }
}
