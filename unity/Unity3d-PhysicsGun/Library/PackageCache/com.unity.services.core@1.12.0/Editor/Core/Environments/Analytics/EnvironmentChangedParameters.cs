using System;
#if UNITY_2023_2_OR_NEWER
using UnityEngine.Analytics;
#endif

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    [Serializable]
    struct EnvironmentChangedParameters
#if UNITY_2023_2_OR_NEWER
    : IAnalytic.IData
#endif
    {
        public string action;
        public string component;
        public string package;
    }
}
