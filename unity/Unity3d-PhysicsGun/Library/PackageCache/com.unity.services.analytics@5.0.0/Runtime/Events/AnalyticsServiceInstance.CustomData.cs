using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        public void CustomData(string eventName)
        {
            CustomData(eventName, null);
        }

        public void CustomData(string eventName, IDictionary<string, object> eventParams)
        {
            CustomData(eventName, eventParams, null, false, false, "AnalyticsServiceInstance.CustomData");
        }

        public void CustomData(string eventName,
            IDictionary<string, object> eventParams,
            Int32? eventVersion,
            bool includeCommonParams,
            bool includePlayerIds,
            string callingMethodIdentifier)
        {
            if (m_IsActive)
            {
                m_DataBuffer.PushStartEvent(eventName, DateTime.Now, eventVersion, includePlayerIds);
                if (includeCommonParams) // i.e. this is a Standard Event
                {
                    m_CommonParams.SerializeCommonEventParams(ref m_DataBuffer, callingMethodIdentifier);
                }

                if (eventParams != null)
                {
                    foreach (KeyValuePair<string, object> paramPair in eventParams)
                    {
                        SerializeObject(eventName, paramPair.Key, paramPair.Value);
                    }
                }

                m_DataBuffer.PushEndEvent();
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            else
            {
                Debug.Log("Did not record custom event " + eventName + " because player has not opted in.");
            }
#endif
        }

        private void SerializeObject(string eventName, string key, object value)
        {
            if (value == null)
            {
                Debug.LogWarning($"Value for key {key} was null, it will not be included in event {eventName}.");
                return;
            }

            /*
             * Had a read of the performance of typeof - the two options were a switch on Type.GetTypeCode(paramType) or
             * the if chain below. Although the if statement involves multiple typeofs, this is supposedly a fairly light
             * operation, and the alternative switch option involved some messy/crazy cases for ints.
             */
            Type paramType = value.GetType();
            if (paramType == typeof(string))
            {
                m_DataBuffer.PushString((string)value, key);
            }
            else if (paramType == typeof(int))
            {
                m_DataBuffer.PushInt((int)value, key);
            }
            else if (paramType == typeof(long))
            {
                m_DataBuffer.PushInt64((long)value, key);
            }
            else if (paramType == typeof(float))
            {
                m_DataBuffer.PushFloat((float)value, key);
            }
            else if (paramType == typeof(double))
            {
                m_DataBuffer.PushDouble((double)value, key);
            }
            else if (paramType == typeof(bool))
            {
                m_DataBuffer.PushBool((bool)value, key);
            }
            else if (paramType == typeof(DateTime))
            {
                m_DataBuffer.PushTimestamp((DateTime)value, key);
            }
            // NOTE: since these are not primitive types, we can't rely on the faster typeof check for these parts.
            else if (value is Enum e)
            {
                m_DataBuffer.PushString(e.ToString(), key);
            }
            else if (value is IDictionary<string, object> dictionary)
            {
                m_DataBuffer.PushObjectStart(key);

                foreach (KeyValuePair<string, object> paramPair in dictionary)
                {
                    SerializeObject(eventName, paramPair.Key, paramPair.Value);
                }

                m_DataBuffer.PushObjectEnd();
            }
            else if (value is IList<object> list)
            {
                if (list.Count > 0)
                {
                    m_DataBuffer.PushArrayStart(key);

                    for (int i = 0; i < list.Count; i++)
                    {
                        SerializeObject(eventName, null, list[i]);
                    }

                    m_DataBuffer.PushArrayEnd();
                }
            }
            else
            {
                Debug.LogError($"Unknown type found for key {key}, this value will not be included in event {eventName}.");
            }
        }
    }
}
