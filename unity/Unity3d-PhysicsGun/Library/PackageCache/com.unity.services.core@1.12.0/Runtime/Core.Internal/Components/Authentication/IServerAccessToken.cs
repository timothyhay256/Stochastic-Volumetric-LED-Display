using Unity.Services.Authentication.Internal;

#if UNITY_2020_2_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace Unity.Services.Authentication.Server.Internal
{
    /// <summary>
    /// Component providing an access token to be used by the server to authorize service requests
    /// </summary>
#if UNITY_2020_2_OR_NEWER
    [RequireImplementors]
#endif
    public interface IServerAccessToken : IAccessToken, IAccessTokenObserver
    {
    }
}
