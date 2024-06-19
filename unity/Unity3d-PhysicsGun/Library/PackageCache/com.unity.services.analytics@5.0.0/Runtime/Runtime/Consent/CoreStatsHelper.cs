namespace Unity.Services.Analytics
{
    interface ICoreStatsHelper
    {
        void SetCoreStatsConsent(bool userProvidedConsent);
    }

    class CoreStatsHelper : ICoreStatsHelper
    {
        public void SetCoreStatsConsent(bool userProvidedConsent)
        {
#if ENABLE_UNITY_GAME_SERVICES_ANALYTICS_SUPPORT
            UnityEngine.Analytics.UGSAnalyticsInternalTools.SetPrivacyStatus(userProvidedConsent);
#endif
        }
    }
}
