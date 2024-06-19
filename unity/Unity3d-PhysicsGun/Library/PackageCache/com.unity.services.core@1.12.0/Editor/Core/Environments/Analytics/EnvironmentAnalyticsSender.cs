using Unity.Services.Core.Internal;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.Services.Core.Editor.Environments.Analytics
{
    class EnvironmentAnalyticsSender : IAnalyticsSender
    {
        public AnalyticsResult SendEvent(object parameters)
        {
            var result = AnalyticsResult.InvalidData;
            if (parameters is EnvironmentChangedParameters environmentChangedParameters)
            {
#if UNITY_2023_2_OR_NEWER
                result = EditorAnalytics.SendAnalytic(new EnvironmentAnalyticEvent(environmentChangedParameters));
#else
                result = EditorAnalytics.SendEventWithLimit(
                    EditorGameServiceAnalyticData.EventName,
                    parameters,
                    EditorGameServiceAnalyticData.Version);
#endif
            }

            LogVerbose(EditorGameServiceAnalyticData.EventName, EditorGameServiceAnalyticData.Version, result, parameters);
            return result;
        }

        static void LogVerbose(string eventName, int eventVersion, AnalyticsResult result, object parameters)
        {
            CoreLogger.LogVerbose($"Sent Analytics Event: {eventName}.v{eventVersion}. Result: {result}. Parameters {parameters.ToString()}");
        }
    }
}
