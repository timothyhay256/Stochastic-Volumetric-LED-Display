using System;
using Unity.Services.Core.Internal;
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace Unity.Services.Authentication.Internal
{
    /// <summary>
    /// Contract for notifying when an access token changes.
    /// </summary>
#if UNITY_2020_2_OR_NEWER
    [RequireImplementors]
#endif
    public interface IAccessTokenObserver : IServiceComponent
    {
        /// <summary>
        /// Event raised when the access token changes.
        /// </summary>
        event Action<string> AccessTokenChanged;
    }
}
