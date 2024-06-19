using Unity.Services.Authentication.Internal;

#if UNITY_2020_2_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace Unity.Services.Authentication.Server.Internal
{
    /// <summary>
    /// Component providing the current environment id to be used by the server
    /// </summary>
#if UNITY_2020_2_OR_NEWER
    [RequireImplementors]
#endif
    public interface IServerEnvironmentId : IEnvironmentId
    {
    }
}