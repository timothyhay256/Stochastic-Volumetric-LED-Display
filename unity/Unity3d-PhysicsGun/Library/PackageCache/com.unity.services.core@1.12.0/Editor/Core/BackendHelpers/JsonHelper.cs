using System;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Internal.Serialization;

namespace Unity.Services.Core.Editor
{
    static class JsonHelper
    {
        public static bool TryJsonDeserialize<T>(this IJsonSerializer self, string json, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                value = self.DeserializeObject<T>(json);
                return true;
            }
            catch (Exception e)
            {
                value = default;
                CoreLogger.LogWarning(e);
            }

            return false;
        }
    }
}
