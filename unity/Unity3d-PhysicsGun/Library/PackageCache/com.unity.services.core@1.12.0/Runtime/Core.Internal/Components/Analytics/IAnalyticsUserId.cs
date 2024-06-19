using Unity.Services.Core.Internal;
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Scripting;
#endif

// We can't use "Unity.Services.Analytics.Internal" because of a compatibility issue with User Reporting 2.0.4.
namespace Unity.Services.Core.Analytics.Internal
{
    /// <summary>
    /// Contract for obtaining the user ID of the analytics pipeline.
    /// </summary>
#if UNITY_2020_2_OR_NEWER
    [RequireImplementors]
#endif
    public interface IAnalyticsUserId : IServiceComponent
    {
        /// <summary>
        /// Returns the user ID value that the Analytics SDK is currently recording events against.
        /// </summary>
        /// <returns>The current Analytics user ID.</returns>
        string GetAnalyticsUserId();
    }
}
