using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Unity.Services.Analytics.Internal
{
    internal interface IBufferSystemCalls
    {
        string GenerateGuid();
        DateTime Now();
    }

    internal interface IBufferIds
    {
        string UserId { get; }
        string InstallId { get; }
        string PlayerId { get; }
        string SessionId { get; }
    }

    class BufferSystemCalls : IBufferSystemCalls
    {
        public string GenerateGuid()
        {
            // NOTE: we are not using .ToByteArray because we do actually need a valid string.
            // Even though the buffer is all bytes, it is ultimately for JSON, so it has to be
            // UTF8 string bytes rather than raw bytes (the string also includes hyphenated
            // subsections).
            return Guid.NewGuid().ToString();
        }

        public DateTime Now()
        {
            return DateTime.Now;
        }
    }

    class BufferX : IBuffer
    {
        // 4MB: 4 * 1024 KB to make an MB and * 1024 bytes to make a KB
        // The Collect endpoint can actually accept payloads of up to 5MB (at time of writing, Jan 2023),
        // but we want to retain some headroom... just in case.
        const long k_UploadBatchMaximumSizeInBytes = 4 * 1024 * 1024;
        const string k_SecondDateFormat = "yyyy-MM-dd HH:mm:ss zzz";
        const string k_MillisecondDateFormat = "yyyy-MM-dd HH:mm:ss.fff zzz";

        readonly byte[] k_WorkingBuffer;
        readonly char[] k_WorkingCharacterBuffer;

        readonly byte[] k_BufferHeader;

        readonly byte[] k_HeaderEventName;
        readonly byte[] k_HeaderUserName;
        readonly byte[] k_HeaderSessionID;
        readonly byte[] k_HeaderEventUUID;
        readonly byte[] k_HeaderTimestamp;

        readonly byte[] k_HeaderEventVersion;
        readonly byte[] k_HeaderInstallationID;
        readonly byte[] k_HeaderPlayerID;

        readonly byte[] k_HeaderOpenEventParams;
        readonly byte[] k_CloseEvent;

        readonly byte k_Quote;
        readonly byte[] k_QuoteColon;
        readonly byte[] k_QuoteComma;
        readonly byte[] k_Comma;
        readonly byte[] k_OpenBrace;
        readonly byte[] k_CloseBraceComma;
        readonly byte[] k_OpenBracket;
        readonly byte[] k_CloseBracketComma;

        readonly byte[] k_True;
        readonly byte[] k_False;

        readonly IBufferSystemCalls m_SystemCalls;
        readonly IDiskCache m_DiskCache;
        IBufferIds m_Ids;

        readonly List<int> m_EventEnds;

        MemoryStream m_SpareBuffer;
        MemoryStream m_Buffer;

        public int Length { get { return (int)m_Buffer.Length; } }

        /// <summary>
        /// The number of events that have been recorded into this buffer.
        /// </summary>
        internal int EventsRecorded { get { return m_EventEnds.Count; } }

        /// <summary>
        /// The byte index of the end of each event blob in the bytestream.
        /// </summary>
        internal IReadOnlyList<int> EventEndIndices => m_EventEnds;

        /// <summary>
        /// The raw contents of the underlying bytestream.
        /// Only exposed for unit testing.
        /// </summary>
        internal byte[] RawContents => m_Buffer.ToArray();

        public BufferX(IBufferSystemCalls eventIdGenerator, IDiskCache diskCache)
        {
            m_Buffer = new MemoryStream((int)k_UploadBatchMaximumSizeInBytes);
            m_SpareBuffer = new MemoryStream((int)k_UploadBatchMaximumSizeInBytes);
            m_EventEnds = new List<int>();

            m_SystemCalls = eventIdGenerator;
            m_DiskCache = diskCache;

            // Transaction receipts can be over 1MB in size, so we really do need working buffers that are this large.
            // Since a single event exceeding the maximum batch size would be eliminated from the buffer, a single
            // string parameter within that event should also never reach that size, so this should be a safe limit.
            k_WorkingBuffer = new byte[k_UploadBatchMaximumSizeInBytes];
            k_WorkingCharacterBuffer = new char[k_UploadBatchMaximumSizeInBytes];

            k_BufferHeader = Encoding.UTF8.GetBytes("{\"eventList\":[");

            k_HeaderEventName = Encoding.UTF8.GetBytes("{\"eventName\":\"");
            k_HeaderUserName = Encoding.UTF8.GetBytes("\",\"userID\":\"");
            k_HeaderSessionID = Encoding.UTF8.GetBytes("\",\"sessionID\":\"");
            k_HeaderEventUUID = Encoding.UTF8.GetBytes("\",\"eventUUID\":\"");
            k_HeaderTimestamp = Encoding.UTF8.GetBytes("\",\"eventTimestamp\":\"");

            k_HeaderEventVersion = Encoding.UTF8.GetBytes("\"eventVersion\":");
            k_HeaderInstallationID = Encoding.UTF8.GetBytes("\"unityInstallationID\":\"");
            k_HeaderPlayerID = Encoding.UTF8.GetBytes("\"unityPlayerID\":\"");

            k_HeaderOpenEventParams = Encoding.UTF8.GetBytes("\"eventParams\":{");

            // Close params block, close event object, comma to prepare for next event
            k_CloseEvent = Encoding.UTF8.GetBytes("}},");

            k_Quote = Encoding.UTF8.GetBytes("\"")[0];
            k_QuoteColon = Encoding.UTF8.GetBytes("\":");
            k_QuoteComma = Encoding.UTF8.GetBytes("\",");
            k_Comma = Encoding.UTF8.GetBytes(",");

            k_OpenBrace = Encoding.UTF8.GetBytes("{");
            k_CloseBraceComma = Encoding.UTF8.GetBytes("},");
            k_OpenBracket = Encoding.UTF8.GetBytes("[");
            k_CloseBracketComma = Encoding.UTF8.GetBytes("],");

            k_True = Encoding.UTF8.GetBytes("true");
            k_False = Encoding.UTF8.GetBytes("false");

            ClearBuffer();
        }

        /// <summary>
        /// For wrangling, IDs are managed by the AnalyticsServiceInstance but the Buffer shouldn't know this.
        /// </summary>
        public void InjectIds(IBufferIds ids)
        {
            m_Ids = ids;
        }

        private void WriteString(in string value)
        {
            int length = Encoding.UTF8.GetBytes(value, 0, Mathf.Min(value.Length, k_WorkingBuffer.Length), k_WorkingBuffer, 0);
            m_Buffer.Write(k_WorkingBuffer, 0, length);
        }

        private void WriteByte(in byte value)
        {
            m_Buffer.WriteByte(value);
        }

        private void WriteBytes(in byte[] bytes)
        {
            m_Buffer.Write(bytes, 0, bytes.Length);
        }

        private void WriteName(string name)
        {
            if (name != null)
            {
                WriteByte(k_Quote);
                WriteString(name);
                WriteBytes(k_QuoteColon);
            }
        }

        public void PushStartEvent(string name, DateTime datetime, long? eventVersion, bool addPlayerIdsToEventBody)
        {
#if UNITY_ANALYTICS_EVENT_LOGS
            Debug.LogFormat("Recording event {0} at {1} (UTC)...", name, SerializeDateTime(datetime));
#endif
            WriteBytes(k_HeaderEventName);
            WriteString(name);
            WriteBytes(k_HeaderUserName);
            WriteString(m_Ids.UserId);
            WriteBytes(k_HeaderSessionID);
            WriteString(m_Ids.SessionId);
            WriteBytes(k_HeaderEventUUID);
            WriteString(m_SystemCalls.GenerateGuid());
            WriteBytes(k_HeaderTimestamp);
            WriteString(SerializeDateTime(datetime));
            WriteBytes(k_QuoteComma);

            if (eventVersion != null)
            {
                WriteBytes(k_HeaderEventVersion);
                WriteString(eventVersion.ToString());
                WriteBytes(k_Comma);
            }

            if (addPlayerIdsToEventBody)
            {
                WriteBytes(k_HeaderInstallationID);
                WriteString(m_Ids.InstallId);
                WriteBytes(k_QuoteComma);

                if (!String.IsNullOrEmpty(m_Ids.PlayerId))
                {
                    WriteBytes(k_HeaderPlayerID);
                    WriteString(m_Ids.PlayerId);
                    WriteBytes(k_QuoteComma);
                }
            }

            WriteBytes(k_HeaderOpenEventParams);
        }

        private void StripTrailingCommaIfNecessary()
        {
            // Stripping the comma once at the end of something is probably
            // faster than checking to see if we need to add one before
            // every single property inside it. Even though it seems
            // a bit convoluted.

            m_Buffer.Seek(-1, SeekOrigin.End);
            char precedingChar = (char)m_Buffer.ReadByte();
            if (precedingChar == ',')
            {
                // Burn that comma, we don't need it and it breaks JSON!
                m_Buffer.Seek(-1, SeekOrigin.Current);
                m_Buffer.SetLength(m_Buffer.Length - 1);
            }
        }

        public void PushEndEvent()
        {
            StripTrailingCommaIfNecessary();

            WriteBytes(k_CloseEvent);

            int bufferLength = (int)m_Buffer.Length;

            // If this event is too big to ever be uploaded, clear the buffer so we don't get stuck forever.
            int eventSize = m_EventEnds.Count > 0 ? bufferLength - m_EventEnds[m_EventEnds.Count - 1] : bufferLength;

            if (eventSize > k_UploadBatchMaximumSizeInBytes)
            {
                Debug.LogWarning($"Detected event that would be too big to upload (greater than {k_UploadBatchMaximumSizeInBytes / 1024}KB in size), discarding it to prevent blockage.");

                int previousBufferLength = m_EventEnds.Count > 0 ? m_EventEnds[m_EventEnds.Count - 1] : k_BufferHeader.Length;

                m_Buffer.SetLength(previousBufferLength);
                m_Buffer.Position = previousBufferLength;
            }
            else
            {
                m_EventEnds.Add(bufferLength);

#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log($"Event {m_EventEnds.Count} ended at: {bufferLength}");
#endif
            }
        }

        public void PushObjectStart(string name = null)
        {
            WriteName(name);
            WriteBytes(k_OpenBrace);
        }

        public void PushObjectEnd()
        {
            StripTrailingCommaIfNecessary();
            WriteBytes(k_CloseBraceComma);
        }

        public void PushArrayStart(string name)
        {
            WriteName(name);
            WriteBytes(k_OpenBracket);
        }

        public void PushArrayEnd()
        {
            StripTrailingCommaIfNecessary();

            WriteBytes(k_CloseBracketComma);
        }

        public void PushDouble(double val, string name = null)
        {
            WriteName(name);
            var formatted = val.ToString(CultureInfo.InvariantCulture);
            WriteString(formatted);
            WriteBytes(k_Comma);
        }

        public void PushFloat(float val, string name = null)
        {
            WriteName(name);
            var formatted = val.ToString(CultureInfo.InvariantCulture);
            WriteString(formatted);
            WriteBytes(k_Comma);
        }

        public void PushString(string value, string name = null)
        {
#if UNITY_ANALYTICS_DEVELOPMENT
            Debug.AssertFormat(!String.IsNullOrEmpty(value), "Required to have a value");
#endif
            if (Encoding.UTF8.GetByteCount(value) < k_WorkingBuffer.Length)
            {
                int c = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    char character = value[i];
                    // Newline, etc.
                    if (Char.IsControl(character))
                    {
                        // Converting to e.g. \U000A rather than \n is not normal, but it is valid JSON.
                        // This gives us a reliable way to escape any control character.
                        // We will allocate a small string here to generate the control code, but it
                        // should be relatively rare (i.e. only if a control char is even present).
                        int codepoint = Convert.ToInt32(character);
                        string control = $"\\U{codepoint:X4}";
                        for (int j = 0; j < control.Length; j++)
                        {
                            k_WorkingCharacterBuffer[c] = control[j];
                            c++;
                        }
                    }
                    // JSON structural characters, quote and slash.
                    else if (character == '"' || character == '\\')
                    {
                        k_WorkingCharacterBuffer[c] = '\\';
                        k_WorkingCharacterBuffer[c + 1] = character;
                        c += 2;
                    }
                    // Normal text
                    else
                    {
                        k_WorkingCharacterBuffer[c] = value[i];
                        c++;
                    }

                    if (c >= k_WorkingCharacterBuffer.Length)
                    {
                        // If working index has tripped over the escaped buffer length, then adding the extra escape codes
                        // has made this value too big to process. We will no longer accept this value.
                        // Truncate the string so it doesn't obliterate your log file.
                        Debug.LogWarning($"String value for field {name} is too long, it will not be recorded.\nValue:\n{value.Substring(0, 128)}...");
                        break;
                    }
                }

                if (c < k_WorkingCharacterBuffer.Length)
                {
                    WriteName(name);
                    WriteByte(k_Quote);

                    int valueLength = Encoding.UTF8.GetBytes(k_WorkingCharacterBuffer, 0, c, k_WorkingBuffer, 0);
                    m_Buffer.Write(k_WorkingBuffer, 0, valueLength);

                    WriteBytes(k_QuoteComma);
                }
            }
            else
            {
                // Truncate the string so it doesn't obliterate your log file.
                Debug.LogWarning($"String value for field \"{name}\" is too long, it will not be recorded.\nValue:\n{value.Substring(0, 128)}...");
            }
        }

        public void PushInt64(long val, string name = null)
        {
            WriteName(name);
            WriteString(val.ToString());
            WriteBytes(k_Comma);
        }

        public void PushInt(int val, string name = null)
        {
            PushInt64(val, name);
        }

        public void PushBool(bool val, string name = null)
        {
            WriteName(name);
            if (val)
            {
                WriteBytes(k_True);
            }
            else
            {
                WriteBytes(k_False);
            }
            WriteBytes(k_Comma);
        }

        public void PushTimestamp(DateTime val, string name)
        {
            WriteName(name);
            WriteByte(k_Quote);
            WriteString(SerializeDateTime(val));
            WriteBytes(k_QuoteComma);
        }

        [Obsolete("This mechanism is no longer supported and will be removed in a future version. Use the new Core IAnalyticsStandardEventComponent API instead.")]
        public void PushEvent(Event evt)
        {
            // Serialize event

            var dateTime = m_SystemCalls.Now();
            PushStartEvent(evt.Name, dateTime, evt.Version, false);

            // Serialize event params

            var eData = evt.Parameters;

            foreach (var data in eData.Data)
            {
                if (data.Value is float f32Val)
                {
                    PushFloat(f32Val, data.Key);
                }
                else if (data.Value is double f64Val)
                {
                    PushDouble(f64Val, data.Key);
                }
                else if (data.Value is string strVal)
                {
                    PushString(strVal, data.Key);
                }
                else if (data.Value is int intVal)
                {
                    PushInt(intVal, data.Key);
                }
                else if (data.Value is Int64 int64Val)
                {
                    PushInt64(int64Val, data.Key);
                }
                else if (data.Value is bool boolVal)
                {
                    PushBool(boolVal, data.Key);
                }
            }

            PushEndEvent();
        }

        public byte[] Serialize()
        {
            if (m_EventEnds.Count > 0)
            {
                long originalBufferPosition = m_Buffer.Position;

                // Tick through the event end indices until we find the last complete event
                // that fits into the maximum payload size.
                int end = m_EventEnds[0];
                int nextEnd = 0;
                while (nextEnd < m_EventEnds.Count &&
                       m_EventEnds[nextEnd] < k_UploadBatchMaximumSizeInBytes)
                {
                    end = m_EventEnds[nextEnd];
                    nextEnd++;
                }

                // Extend the payload so we can fit the suffix.
                byte[] payload = new byte[end + 1];
                m_Buffer.Position = 0;
                m_Buffer.Read(payload, 0, end);

                // NOTE: the final character will be a comma that we don't want,
                // so take this opportunity to overwrite it with the closing
                // bracket (event list) and brace (payload object).
                byte[] suffix = Encoding.UTF8.GetBytes("]}");
                payload[end - 1] = suffix[0];
                payload[end] = suffix[1];

                m_Buffer.Position = originalBufferPosition;

                return payload;
            }
            else
            {
                return null;
            }
        }

        public void ClearBuffer()
        {
            m_Buffer.SetLength(0);
            m_Buffer.Position = 0;
            WriteBytes(k_BufferHeader);

            m_EventEnds.Clear();
        }

        public void ClearBuffer(long upTo)
        {
            MemoryStream oldBuffer = m_Buffer;
            m_Buffer = m_SpareBuffer;
            m_SpareBuffer = oldBuffer;

            // We want to keep the end markers for events that have been copied over.
            // We have to account for the start point change AND remove markers for events before the clear point.

            int lastClearedEventIndex = 0;
            for (int i = 0; i < m_EventEnds.Count; i++)
            {
                m_EventEnds[i] = m_EventEnds[i] - (int)upTo + k_BufferHeader.Length;
                if (m_EventEnds[i] <= k_BufferHeader.Length)
                {
                    lastClearedEventIndex = i;
                }
            }
            m_EventEnds.RemoveRange(0, lastClearedEventIndex + 1);

            // Reset the buffer back to a blank state...
            m_Buffer.SetLength(0);
            m_Buffer.Position = 0;
            WriteBytes(k_BufferHeader);

            // ... and copy over anything that came after the cut-off point.
            m_SpareBuffer.Position = upTo;
            for (long i = upTo; i < m_SpareBuffer.Length; i++)
            {
                byte b = (byte)m_SpareBuffer.ReadByte();
                m_Buffer.WriteByte(b);
            }

            m_SpareBuffer.SetLength(0);
            m_SpareBuffer.Position = 0;
        }

        public void FlushToDisk()
        {
            m_DiskCache.Write(m_EventEnds, m_Buffer);
        }

        public void ClearDiskCache()
        {
            m_DiskCache.Clear();
        }

        public void LoadFromDisk()
        {
            bool success = m_DiskCache.Read(m_EventEnds, m_Buffer);

            if (!success)
            {
                // Reset the buffer in case we failed half-way through populating it.
                ClearBuffer();
            }
        }

        internal static string SerializeDateTime(DateTime dateTime)
        {
            return dateTime.ToString(k_MillisecondDateFormat, CultureInfo.InvariantCulture);
        }
    }
}
