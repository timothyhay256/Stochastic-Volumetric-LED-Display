using System;

namespace Unity.Services.Analytics.Internal
{
    interface IBuffer
    {
        int Length { get; }
        byte[] Serialize();
        void PushStartEvent(string name, DateTime datetime, Int64? eventVersion, bool addPlayerIdsToEventBody = false);
        void PushEndEvent();
        void PushObjectStart(string name = null);
        void PushObjectEnd();
        void PushArrayStart(string name);
        void PushArrayEnd();
        void PushDouble(double val, string name = null);
        void PushFloat(float val, string name = null);
        void PushString(string val, string name = null);
        void PushInt64(Int64 val, string name = null);
        void PushInt(int val, string name = null);
        void PushBool(bool val, string name = null);
        void PushTimestamp(DateTime val, string name = null);
        void FlushToDisk();
        void ClearDiskCache();
        void ClearBuffer();
        void ClearBuffer(long upTo);
        void LoadFromDisk();

        [Obsolete("This mechanism is no longer supported and will be removed in a future version. Use the new Core IAnalyticsStandardEventComponent API instead.")]
        void PushEvent(Event evt);
    }
}
