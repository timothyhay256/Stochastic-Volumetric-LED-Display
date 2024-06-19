using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.Analytics.Data;
using Unity.Services.Analytics.Internal;
using Unity.Services.Analytics.Platform;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Device.Internal;
using UnityEngine;
using Event = Unity.Services.Analytics.Internal.Event;

namespace Unity.Services.Analytics
{
    internal interface IAnalyticsServiceSystemCalls
    {
        DateTime UtcNow { get; }
    }

    internal class AnalyticsServiceSystemCalls : IAnalyticsServiceSystemCalls
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }

    internal interface IUnstructuredEventRecorder
    {
        void CustomData(string eventName,
            IDictionary<string, object> eventParams,
            Int32? eventVersion,
            bool includeCommonParams,
            bool includePlayerIds,
            string callingMethodIdentifier);
    }

    partial class AnalyticsServiceInstance : IAnalyticsService, IUnstructuredEventRecorder, IBufferIds
    {
        internal enum ConsentFlow
        {
            Neither,
            Old,
            New
        }

        public string PrivacyUrl => "https://unity3d.com/legal/privacy-policy";

        const string k_ForgetCallingId = "com.unity.services.analytics.Events." + nameof(OptOut);
        const string m_StartUpCallingId = "com.unity.services.analytics.Events.Startup";

        readonly TimeSpan k_BackgroundSessionRefreshPeriod = TimeSpan.FromMinutes(5);

        readonly StdCommonParams m_CommonParams;
        readonly IPlayerId m_PlayerId;
        readonly IInstallationId m_InstallId;
        readonly IDataGenerator m_DataGenerator;
        readonly ICoreStatsHelper m_CoreStatsHelper;
        readonly IConsentTracker m_ConsentTracker;
        readonly IDispatcher m_DataDispatcher;
        readonly IAnalyticsForgetter m_AnalyticsForgetter;
        readonly IExternalUserId m_CustomUserId;
        readonly IAnalyticsServiceSystemCalls m_SystemCalls;
        readonly IAnalyticsContainer m_Container;

        internal IBuffer m_DataBuffer;

        internal string CustomAnalyticsId { get { return m_CustomUserId.UserId; } }

        public string SessionID { get; private set; }

        int m_BufferLengthAtLastGameRunning;
        DateTime m_ApplicationPauseTime;

        bool m_IsActive;
        ConsentFlow m_ConsentFlow;

        /// <summary>
        /// This is for internal unit test usage only.
        /// In the real world, use Activate() and Deactivate().
        /// </summary>
        internal bool Active
        {
            get { return m_IsActive; }
            set { m_IsActive = value; }
        }

        /// <summary>
        /// This is for internal unit test usage only.
        /// In the real world, flow is selected by calling OptIn() or CheckForRequiredConsents().
        /// </summary>
        internal ConsentFlow SelectedConsentFlow
        {
            get { return m_ConsentFlow; }
            set { m_ConsentFlow = value; }
        }

        public string UserId { get { return GetAnalyticsUserID(); } }
        public string InstallId { get { return m_InstallId.GetOrCreateIdentifier(); } }
        public string PlayerId { get { return m_PlayerId?.PlayerId; } }
        public string SessionId { get { return SessionID; } }

        internal AnalyticsServiceInstance(IDataGenerator dataGenerator,
                                          IBuffer realBuffer,
                                          ICoreStatsHelper coreStatsHelper,
                                          IConsentTracker consentTracker,
                                          IDispatcher dispatcher,
                                          IAnalyticsForgetter forgetter,
                                          IInstallationId installId,
                                          IPlayerId playerId,
                                          string environment,
                                          IExternalUserId customAnalyticsId,
                                          IAnalyticsServiceSystemCalls systemCalls,
                                          IAnalyticsContainer container)
        {
            m_CustomUserId = customAnalyticsId;

            m_DataGenerator = dataGenerator;
            m_SystemCalls = systemCalls;

            m_CoreStatsHelper = coreStatsHelper;
            m_ConsentTracker = consentTracker;
            m_DataDispatcher = dispatcher;
            m_Container = container;

            m_DataBuffer = realBuffer;
            m_DataDispatcher.SetBuffer(realBuffer);
            m_DataGenerator.SetBuffer(realBuffer);

            m_IsActive = false;

            m_AnalyticsForgetter = forgetter;

            m_CommonParams = new StdCommonParams
            {
                ClientVersion = Application.version,
                ProjectID = Application.cloudProjectId,
                GameBundleID = Application.identifier,
                Platform = Runtime.Name(),
                BuildGuuid = Application.buildGUID,
                Idfv = SystemInfo.deviceUniqueIdentifier
            };

            m_InstallId = installId;
            m_PlayerId = playerId;

            RefreshSessionID();

#if UNITY_ANALYTICS_DEVELOPMENT
            Debug.LogFormat("UA2 Setup SessionID: {0}", SessionID);
#endif
        }

        internal void ResumeDataDeletionIfNecessary()
        {
            if (m_AnalyticsForgetter.DeletionInProgress)
            {
                DeactivateWithDataDeletionRequest();
            }
        }

        async Task InitializeUser()
        {
            SetVariableCommonParams();

            try
            {
                await m_ConsentTracker.CheckGeoIP();

                if (m_ConsentTracker.IsGeoIpChecked() && (m_ConsentTracker.IsConsentDenied() || m_ConsentTracker.IsOptingOutInProgress()))
                {
                    OptOut();
                }
            }
#if UNITY_ANALYTICS_EVENT_LOGS
            catch (ConsentCheckException e)
            {
                Debug.Log("Initial GeoIP lookup fail: " + e.Message);
            }
#else
            catch (ConsentCheckException)
            {
            }
#endif
        }

        public void StartDataCollection()
        {
            // The New flow allows "opt out and back in again", so this method can be activated
            // repeatedly within a single session. It should do nothing if the SDK is already
            // active, but otherwise (re)activate the SDK as normal.
            if (m_ConsentFlow == ConsentFlow.Neither ||
                m_ConsentFlow == ConsentFlow.New)
            {
                m_ConsentFlow = ConsentFlow.New;

                if (!m_IsActive)
                {
                    // In case you had previously requested data deletion, you must now be able to request it again.
                    m_AnalyticsForgetter.ResetDataDeletionStatus();
                    m_CoreStatsHelper.SetCoreStatsConsent(true);

                    Activate();
                }
            }
            else if (m_ConsentFlow == ConsentFlow.Old)
            {
                throw new NotSupportedException("The OptIn method cannot be used under the old consent flow.");
            }
        }

        void Activate()
        {
            if (!m_IsActive)
            {
                m_IsActive = true;
                m_Container.Enable();
                m_DataBuffer.LoadFromDisk();

                RecordStartupEvents();

                Flush();
            }
        }

        public void StopDataCollection()
        {
            if (m_ConsentFlow == ConsentFlow.New)
            {
                if (m_IsActive)
                {
                    m_DataDispatcher.Flush();
                    Deactivate();
                }
            }
            else if (m_ConsentFlow == ConsentFlow.Old)
            {
                throw new NotSupportedException("The StopDataCollection() method cannot be used under the old consent flow. Please see the migration guide for more information: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide");
            }
            else
            {
                throw new NotSupportedException("The StopDataCollection() method cannot be used before StartDataCollection() has been called.");
            }
        }

        internal void DeactivateWithDataDeletionRequest()
        {
            m_DataBuffer.ClearBuffer();
            m_DataBuffer.ClearDiskCache();
            m_Container.Enable();
            m_AnalyticsForgetter.AttemptToForget(UserId, InstallId, PlayerId, BufferX.SerializeDateTime(DateTime.Now), k_ForgetCallingId, DataDeletionCompleted);

            Deactivate();
        }

        void DataDeletionCompleted()
        {
            if (!m_IsActive)
            {
                m_Container.Disable();
            }
        }

        void Deactivate()
        {
            if (m_IsActive)
            {
                m_IsActive = false;

                if ((m_ConsentFlow == ConsentFlow.New && !m_AnalyticsForgetter.DeletionInProgress) ||
                    (m_ConsentFlow == ConsentFlow.Old && !m_ConsentTracker.IsOptingOutInProgress()))
                {
                    // Only disable the container if opting out is not in progress. Otherwise, leave it
                    // running so that the heartbeat can re-attempt the deletion request upload until
                    // it succeeds.
                    m_Container.Disable();
                }
            }

            m_CoreStatsHelper.SetCoreStatsConsent(false);
        }

        bool m_StartUpEventsRecorded = false;
        void RecordStartupEvents()
        {
            if (!m_StartUpEventsRecorded)
            {
                // Only record start-up events once in a session, even if the player opts in/out/in again.
                m_StartUpEventsRecorded = true;

                // Startup Events.
                m_DataGenerator.SdkStartup(DateTime.Now, m_CommonParams, m_StartUpCallingId);
                m_DataGenerator.ClientDevice(DateTime.Now, m_CommonParams, m_StartUpCallingId, SystemInfo.processorType, SystemInfo.graphicsDeviceName, SystemInfo.processorCount, SystemInfo.systemMemorySize, Screen.width, Screen.height, (int)Screen.dpi);

#if UNITY_DOTSRUNTIME
                var isTiny = true;
#else
                var isTiny = false;
#endif

                m_DataGenerator.GameStarted(DateTime.Now, m_CommonParams, m_StartUpCallingId, Application.buildGUID, SystemInfo.operatingSystem, isTiny, DebugDevice.IsDebugDevice(), Locale.AnalyticsRegionLanguageCode());

                if (m_InstallId != null && new InternalNewPlayerHelper(m_InstallId).IsNewPlayer())
                {
                    m_DataGenerator.NewPlayer(DateTime.Now, m_CommonParams, m_StartUpCallingId, SystemInfo.deviceModel);
                }
            }
        }

        public string GetAnalyticsUserID()
        {
            return !String.IsNullOrEmpty(CustomAnalyticsId) ? CustomAnalyticsId : m_InstallId.GetOrCreateIdentifier();
        }

        internal void ApplicationPaused(bool paused)
        {
            if (paused)
            {
                m_ApplicationPauseTime = m_SystemCalls.UtcNow;
#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Analytics SDK detected application pause at: " + m_ApplicationPauseTime.ToString());
#endif
            }
            else
            {
                DateTime now = m_SystemCalls.UtcNow;

#if UNITY_ANALYTICS_DEVELOPMENT
                Debug.Log("Analytics SDK detected application unpause at: " + now);
#endif
                if (now > m_ApplicationPauseTime + k_BackgroundSessionRefreshPeriod)
                {
                    RefreshSessionID();
                }
            }
        }

        internal void RefreshSessionID()
        {
            SessionID = Guid.NewGuid().ToString();

#if UNITY_ANALYTICS_DEVELOPMENT
            Debug.Log("Analytics SDK started new session: " + SessionID);
#endif
        }

        internal int AutoflushPeriodMultiplier
        {
            get { return Mathf.Clamp(1 + m_DataDispatcher.ConsecutiveFailedUploadCount, 1, 8); }
        }

        public void Flush()
        {
            if (m_IsActive)
            {
                switch (m_ConsentFlow)
                {
                    case ConsentFlow.Old:
                        if (m_ConsentTracker.IsGeoIpChecked() && m_ConsentTracker.IsConsentGiven())
                        {
                            m_DataDispatcher.Flush();
                        }
                        else
                        {
                            // Also, check if the consent was definitely checked and given at this point.
                            Debug.LogWarning("Required consent wasn't checked and given when trying to dispatch events, the events cannot be sent.");
                        }

                        if (m_ConsentTracker.IsOptingOutInProgress())
                        {
                            m_AnalyticsForgetter.AttemptToForget(UserId, InstallId, PlayerId, BufferX.SerializeDateTime(DateTime.Now), k_ForgetCallingId, OldForgetMeEventUploaded);
                        }
                        break;
                    case ConsentFlow.New:
                        // No need for conditional guard, m_IsActive is only true if we are clear to flush.
                        m_DataDispatcher.Flush();
                        break;
                }
            }
            else if (m_AnalyticsForgetter.DeletionInProgress)
            {
                DeactivateWithDataDeletionRequest();
            }
        }

        public void RequestDataDeletion()
        {
            DeactivateWithDataDeletionRequest();
        }

        public void RecordInternalEvent(Event eventToRecord)
        {
            if (m_IsActive)
            {
                m_DataBuffer.PushEvent(eventToRecord);
            }
        }

        internal void ApplicationQuit()
        {
            if (m_IsActive)
            {
                m_DataGenerator.GameEnded(DateTime.Now, m_CommonParams, "com.unity.services.analytics.Events.Shutdown", DataGenerator.SessionEndState.QUIT);

                // Flush to disk before attempting final upload, in case we do not have enough time during teardown
                // to make the request and/or determine its success (e.g. if we shut down offline)
                m_DataBuffer.FlushToDisk();

                Flush();
            }
        }

        internal void RecordGameRunningIfNecessary()
        {
            if (m_IsActive)
            {
                if (m_DataBuffer.Length == 0 || m_DataBuffer.Length == m_BufferLengthAtLastGameRunning)
                {
                    SetVariableCommonParams();
                    m_DataGenerator.GameRunning(DateTime.Now, m_CommonParams, "com.unity.services.analytics.AnalyticsServiceInstance.RecordGameRunningIfNecessary");
                    m_BufferLengthAtLastGameRunning = m_DataBuffer.Length;
                }
                else
                {
                    m_BufferLengthAtLastGameRunning = m_DataBuffer.Length;
                }
            }
        }

        void SetVariableCommonParams()
        {
            // TODO: these should be updated on serialising any event that contains them,
            // not at sporadic points throughout the lifecycle. We seem to have lost track of
            // these during successive refactorings.
            m_CommonParams.DeviceVolume = DeviceVolumeProvider.GetDeviceVolume();
            m_CommonParams.BatteryLoad = SystemInfo.batteryLevel;
            m_CommonParams.UasUserID = m_PlayerId?.PlayerId;
        }

        public async Task SetAnalyticsEnabled(bool enabled)
        {
            if (enabled && !m_IsActive)
            {
                Activate();
            }
            else if (!enabled && m_IsActive)
            {
                Deactivate();
            }

            // For backwards compatibility.
            await Task.CompletedTask;
        }
    }
}
