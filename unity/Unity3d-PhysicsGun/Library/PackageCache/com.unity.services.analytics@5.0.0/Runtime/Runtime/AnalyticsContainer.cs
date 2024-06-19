using System;
using UnityEngine;

namespace Unity.Services.Analytics
{
    internal interface IAnalyticsContainer
    {
        void Initialize(AnalyticsServiceInstance service);
        void Enable();
        void Disable();
    }

    internal class AnalyticsContainer : MonoBehaviour, IAnalyticsContainer
    {
        const float k_AutoFlushPeriod = 60.0f;
        const float k_GameRunningPeriod = 60.0f;

        static bool s_Created;
        static GameObject s_Container;

        float m_AutoFlushTime = 0.0f;
        float m_GameRunningTime = 0.0f;
        AnalyticsServiceInstance m_Service;

        float AutoFlushPeriod
        {
            get
            {
                return k_AutoFlushPeriod * m_Service.AutoflushPeriodMultiplier;
            }
        }

        /// <summary>
        /// For the test harness only.
        /// </summary>
        internal static AnalyticsContainer Instance { get; private set; }
        /// <summary>
        /// For the test harness only.
        /// </summary>
        internal float TimeUntilHeartbeat => AutoFlushPeriod - m_AutoFlushTime;

        internal static AnalyticsContainer CreateContainer()
        {
            if (!s_Created)
            {
#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Created Analytics Container");
#endif

                s_Container = new GameObject("AnalyticsContainer");
                Instance = s_Container.AddComponent<AnalyticsContainer>();

                s_Container.hideFlags = HideFlags.DontSaveInBuild | HideFlags.NotEditable;
#if !UNITY_ANALYTICS_DEVELOPMENT
                s_Container.hideFlags |= HideFlags.HideInInspector;
#endif

                DontDestroyOnLoad(s_Container);
                s_Created = true;
            }

            return Instance;
        }

        public void Initialize(AnalyticsServiceInstance service)
        {
            m_Service = service;
            enabled = false;
        }

        public void Enable()
        {
            enabled = true;
        }

        public void Disable()
        {
            enabled = false;
            m_AutoFlushTime = 0.0f;
        }

        void Update()
        {
            // Use unscaled time in case the user sets timeScale to anything other than 1 (e.g. to 0 to pause their game),
            // we always want to record gameRunning/do batch upload on the same real-time cadence regardless of framerate
            // or user interference.

            m_GameRunningTime += Time.unscaledDeltaTime;
            if (m_GameRunningTime >= k_GameRunningPeriod)
            {
                m_Service.RecordGameRunningIfNecessary();
                m_GameRunningTime = 0.0f;
            }

            m_AutoFlushTime += Time.unscaledDeltaTime;
            if (m_AutoFlushTime >= AutoFlushPeriod)
            {
                m_Service.Flush();
                m_AutoFlushTime = 0.0f;
            }
        }

        void OnApplicationPause(bool paused)
        {
            m_Service.ApplicationPaused(paused);
        }

        void OnDestroy()
        {
            // NOTE: we use OnDestroy rather than OnApplicationQuit in case the game developer should
            // deliberately/accidentally destroy the container object. This should ensure graceful shutdown
            // of the SDK regardless of 'how' it actually got turned off.

            m_Service.ApplicationQuit();

            s_Container = null;
            s_Created = false;
        }
    }
}
