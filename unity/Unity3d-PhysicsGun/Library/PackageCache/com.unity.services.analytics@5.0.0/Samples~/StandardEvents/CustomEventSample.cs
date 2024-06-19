using System;
using System.Collections.Generic;

namespace Unity.Services.Analytics
{
    public static class CustomEventSample
    {
        public static void RecordCustomEventWithNoParameters()
        {
            // NOTE: this will show up on the dashboard as an invalid event, unless
            // you have created a schema that matches it.
            AnalyticsService.Instance.CustomData("myEvent");
        }

        public static void RecordCustomEventWithParameters()
        {
            // NOTE: this will show up on the dashboard as an invalid event, unless
            // you have created a schema that matches it.
            var parameters = new Dictionary<string, object>
            {
                { "fabulousString", "hello there" },
                { "sparklingInt", 1337 },
                { "tremendousLong", Int64.MaxValue },
                { "spectacularFloat", 0.451f },
                { "incredibleDouble", 0.000000000000000031337 },
                { "peculiarBool", true }
            };

            AnalyticsService.Instance.CustomData("myEvent", parameters);
        }
    }
}
