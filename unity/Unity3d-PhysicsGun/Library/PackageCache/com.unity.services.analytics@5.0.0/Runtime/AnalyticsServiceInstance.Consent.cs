using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics.Internal;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Services.Analytics
{
    partial class AnalyticsServiceInstance
    {
        public async Task<List<string>> CheckForRequiredConsents()
        {
            if (m_ConsentFlow == ConsentFlow.New)
            {
                throw new NotSupportedException("The CheckForRequiredConsents method cannot be used under the new consent flow.");
            }
            else
            {
                m_ConsentFlow = ConsentFlow.Old;

                await InitializeUser();

                var response = await m_ConsentTracker.CheckGeoIP();

                // Consent is not required. We are clear to proceed.
                // Automatically activate the SDK.
                if (response.identifier == Consent.None)
                {
                    Activate();
                    return new List<string>();
                }
                else
                {
                    // Consent is required.

                    // Consent has previously been denied, from value in PlayerPrefs.
                    // Do not activate the SDK.
                    if (m_ConsentTracker.IsConsentDenied())
                    {
                        return new List<string>();
                    }

                    // Consent has not yet been given or denied. No value in PlayerPrefs.
                    // Do not activate the SDK. It will be activated by using ProvideOptInConsent(...).
                    if (!m_ConsentTracker.IsConsentGiven())
                    {
                        return new List<string> { response.identifier };
                    }

                    // Consent has been given previously, from value in PlayerPrefs. We are clear to proceed.
                    // Automatically activate the SDK.
                    Activate();
                    return new List<string>();
                }
            }
        }

        public void ProvideOptInConsent(string identifier, bool consent)
        {
            if (m_ConsentFlow == ConsentFlow.New)
            {
                throw new NotSupportedException("The ProvideOptInConsent method cannot be used under the new consent flow.");
            }
            else
            {
                m_ConsentFlow = ConsentFlow.Old;

                m_CoreStatsHelper.SetCoreStatsConsent(consent);

                if (!m_ConsentTracker.IsGeoIpChecked())
                {
                    throw new ConsentCheckException(ConsentCheckExceptionReason.ConsentFlowNotKnown,
                        CommonErrorCodes.Unknown,
                        "The required consent flow cannot be determined. Make sure CheckForRequiredConsents() method was successfully called.",
                        null);
                }

                if (consent == false)
                {
                    if (m_ConsentTracker.IsConsentGiven(identifier))
                    {
                        m_ConsentTracker.BeginOptOutProcess(identifier);
                        RevokeWithForgetEvent();
                        return;
                    }
                }

                m_ConsentTracker.SetUserConsentStatus(identifier, consent);
                if (consent)
                {
                    Activate();
                }
                else
                {
                    Deactivate();
                }
            }
        }

        public void OptOut()
        {
            if (m_ConsentFlow == ConsentFlow.New)
            {
                throw new NotSupportedException("The OptOut() method cannot be used under the new consent flow. Please see the migration guide for more information: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide");
            }
            else
            {
                Debug.Log(m_ConsentTracker.IsConsentDenied()
                    ? "This user has opted out. Any cached events have been discarded and no more will be collected."
                    : "This user has opted out and is in the process of being forgotten...");

                if (m_ConsentTracker.IsConsentGiven())
                {
                    // We have revoked consent but have not yet sent the ForgetMe signal
                    // Thus we need to keep some of the dispatcher alive until that is done
                    m_ConsentTracker.BeginOptOutProcess();
                    RevokeWithForgetEvent();

                    return;
                }

                if (m_ConsentTracker.IsOptingOutInProgress())
                {
                    RevokeWithForgetEvent();
                    return;
                }

                Deactivate();
                m_ConsentTracker.SetDenyConsentToAll();
                m_CoreStatsHelper.SetCoreStatsConsent(false);
            }
        }

        internal void RevokeWithForgetEvent()
        {
            m_AnalyticsForgetter.AttemptToForget(UserId, InstallId, PlayerId, BufferX.SerializeDateTime(DateTime.Now), k_ForgetCallingId, OldForgetMeEventUploaded);

            Deactivate();
        }

        internal void OldForgetMeEventUploaded()
        {
            m_ConsentTracker.FinishOptOutProcess();

            if (!m_IsActive)
            {
                m_Container.Disable();
            }

#if UNITY_ANALYTICS_EVENT_LOGS
            Debug.Log("User opted out successfully and has been forgotten!");
#endif
        }
    }
}
