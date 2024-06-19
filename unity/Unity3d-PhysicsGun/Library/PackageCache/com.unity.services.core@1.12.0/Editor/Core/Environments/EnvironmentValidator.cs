using System.Threading.Tasks;
using Unity.Services.Core.Editor.Shared.Clients;

namespace Unity.Services.Core.Editor.Environments
{
    class EnvironmentValidator : IEnvironmentValidator
    {
        readonly IProjectInfo m_ProjectInfo;
        readonly IEnvironmentFetcher m_EnvironmentFetcher;
        readonly IEnvironmentService m_EnvironmentService;
        readonly IAccessTokens m_AccessTokens;

        public EnvironmentValidator(
            IProjectInfo projectInfo,
            IEnvironmentFetcher fetcher,
            IEnvironmentService environmentService,
            IAccessTokens accessTokens)
        {
            m_EnvironmentService = environmentService;
            m_EnvironmentFetcher = fetcher;
            m_ProjectInfo = projectInfo;
            m_AccessTokens = accessTokens;
        }

        public async Task<ValidationResult> ValidateEnvironmentAsync()
        {
            var projectId = m_ProjectInfo.ProjectId;
            var gatewayToken = await m_AccessTokens.GetServicesGatewayTokenAsync();
            var environmentId = m_EnvironmentService.ActiveEnvironmentId.ToString();

            string errorMessage = null;
            if (string.IsNullOrEmpty(environmentId))
            {
                errorMessage = "Environment is not set! Please set it through the Environment Selector at Edit -> Project Settings -> Deployment";
            }
            else if (string.IsNullOrEmpty(projectId))
            {
                errorMessage = "Project is not linked! Please make sure that you have properly linked a project.";
            }
            else if (string.IsNullOrEmpty(gatewayToken))
            {
                errorMessage = "Unable to get login credentials! Please make sure that you have properly linked a project.";
            }
            else if (!await EnvironmentExistsInProjectAsync(environmentId))
            {
                errorMessage = "Environment does not exist in the current project!";
            }

            return new ValidationResult(errorMessage);
        }

        async Task<bool> EnvironmentExistsInProjectAsync(string environmentId)
        {
            try
            {
                var env = await m_EnvironmentFetcher.FetchEnvironment(environmentId);
                return env.HasValue;
            }
            catch
            {
                return false;
            }
        }
    }
}
