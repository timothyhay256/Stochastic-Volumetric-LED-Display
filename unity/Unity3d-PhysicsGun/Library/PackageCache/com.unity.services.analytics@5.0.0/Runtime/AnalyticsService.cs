using System;
using Unity.Services.Core;

namespace Unity.Services.Analytics
{
    public static class AnalyticsService
    {
        internal static AnalyticsServiceInstance internalInstance;

        public static IAnalyticsService Instance
        {
            get
            {
                if (internalInstance == null)
                {
                    throw new ServicesInitializationException("The Analytics service has not been initialized. Please initialize Unity Services.");
                }

                return internalInstance;
            }
        }
    }
}
