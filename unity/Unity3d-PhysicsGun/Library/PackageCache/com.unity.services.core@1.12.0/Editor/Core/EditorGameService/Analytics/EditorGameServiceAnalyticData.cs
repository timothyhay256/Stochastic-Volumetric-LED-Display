using System;

#if UNITY_2023_2_OR_NEWER
using UnityEngine.Analytics;
#endif

namespace Unity.Services.Core.Editor
{
    [Serializable]
    struct EditorGameServiceAnalyticData
#if UNITY_2023_2_OR_NEWER
        : IAnalytic.IData
#endif
    {
        internal const int Version = 1;
        internal const string EventName = "editorgameserviceeditor";

        public string action;
        public string component;
        public string package;
    }
}
