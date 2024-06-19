using System.Threading.Tasks;

namespace Unity.Services.Core.Editor
{
    /// <summary>
    /// Helper class to get the different kind of tokens used by services at editor time.
    /// </summary>
    public class AccessTokens : IAccessTokens
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="AccessTokens"/> class.
        /// </summary>
        public AccessTokens()
        {
        }

        /// <inheritdoc cref="IAccessTokens.GetGenesisToken"/>
        public static string GetGenesisToken() => AccessTokensSingleton.Instance.GetGenesisToken();

        /// <inheritdoc cref="IAccessTokens.GetServicesGatewayTokenAsync"/>
        public Task<string> GetServicesGatewayTokenAsync()
        {
            return AccessTokensSingleton.Instance.GetServicesGatewayTokenAsync();
        }

        /// <inheritdoc/>
        string IAccessTokens.GetGenesisToken() => GetGenesisToken();
    }
}
