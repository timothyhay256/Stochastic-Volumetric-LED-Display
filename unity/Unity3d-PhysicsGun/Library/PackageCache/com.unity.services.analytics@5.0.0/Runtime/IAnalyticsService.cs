using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics.Internal;

namespace Unity.Services.Analytics
{
    public interface IAnalyticsService
    {
        /// <summary>
        /// This is the URL for the Unity Analytics privacy policy. This policy page should
        /// be presented to the user in a platform-appropriate way along with the ability to
        /// opt out of data collection.
        /// </summary>
        string PrivacyUrl { get; }

        /// <summary>
        /// Forces an immediately upload of all recorded events to the server, if there is an internet connection and a flush is not already in progress.
        /// Flushing is triggered automatically on a regular cadence so you should not need to use this method, unless you specifically require some
        /// queued events to be uploaded immediately.
        /// </summary>
        /// <exception cref="ConsentCheckException">Thrown if the required consent flow cannot be determined..</exception>
        void Flush();

        /// <summary>
        /// Record an adImpression event, if the player has opted in to data collection (see OptIn method).
        /// </summary>
        /// <param name="parameters">(Required) Helper object to handle parameters.</param>
        void AdImpression(AdImpressionParameters parameters);

        /// <summary>
        /// Record a transaction event, if the player has opted in to data collection (see OptIn method).
        /// </summary>
        /// <param name="transactionParameters">(Required) Helper object to handle parameters.</param>
        void Transaction(TransactionParameters transactionParameters);

        /// <summary>
        /// Record a transactionFailed event, if the player has opted in to data collection.
        /// </summary>
        /// <param name="parameters">(Required) Helper object to handle parameters.</param>
        void TransactionFailed(TransactionFailedParameters parameters);

        /// <summary>
        /// Record a custom event, if the player has opted in to data collection (see OptIn method).
        ///
        /// A schema for this event must exist on the dashboard or it will be ignored.
        /// </summary>
        void CustomData(string eventName, IDictionary<string, object> eventParams);

        /// <summary>
        /// Record a custom event that does not have any parameters, if the player has opted in to data collection (see OptIn method).
        ///
        /// A schema for this event must exist on the dashboard or it will be ignored.
        /// </summary>
        void CustomData(string eventName);

        /// <summary>
        /// Signals that consent has been obtained from the player and enables data collection.
        ///
        /// By calling this method you confirm that consent has been obtained or is not required from the player under any applicable
        /// data privacy laws (e.g. GDPR in Europe, PIPL in China). Please obtain your own legal advice to ensure you are in compliance
        /// with any data privacy laws regarding personal data collection in the territories in which your app is available.
        /// </summary>
        void StartDataCollection();

        /// <summary>
        /// Returns identifiers of required consents we need to gather from the user
        /// in order to be allowed to sent analytics events.
        /// This method must be called every time the game starts - without checking the geolocation,
        /// no event will be sent (even if the consent was already given).
        /// If the required consent was already given, an empty list is returned.
        /// If the user already opted out from the current legislation, an empty list is returned.
        /// It involves the GeoIP call.
        /// `ConsentCheckException` is thrown if the GeoIP call was unsuccessful.
        ///
        /// </summary>
        /// <returns>A list of consent identifiers that are required for sending analytics events.</returns>
        /// <exception cref="ConsentCheckException">Thrown if the GeoIP call was unsuccessful.</exception>
        [Obsolete("This method is part of the old consent flow and should no longer be used. For more information, please see the migration guide: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide")]
        Task<List<string>> CheckForRequiredConsents();

        /// <summary>
        /// Sets the consent status for the specified opt-in-based legislation (PIPL etc).
        /// The required legislation identifier can be found by calling `CheckForRequiredConsents` method.
        /// If this method is tried to be used for the incorrect legislation (PIPL outside China etc),
        /// the `ConsentCheckException` is thrown.
        ///
        /// </summary>
        /// <param name="identifier">The legislation identifier for which the consent status should be changed.</param>
        /// <param name="consent">The consent status which should be set for the specified legislation.</param>
        /// <exception cref="ConsentCheckException">Thrown if the incorrect legislation was being provided or
        /// the required consent flow cannot be determined.</exception>
        [Obsolete("This method is part of the old consent flow and should no longer be used. For more information, please see the migration guide: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide")]
        void ProvideOptInConsent(string identifier, bool consent);

        /// <summary>
        /// Opts the user out of sending analytics and disables the SDK.
        /// A final 'forget me' signal will be uploaded which will trigger purge of analytics data for this user from the back-end.
        /// If this 'forget me' event cannot be uploaded immediately (e.g. due to network outage), it will be reattempted regularly
        /// until successful upload is confirmed. This status is mainted between sessions to ensure that the signal will be uploaded
        /// eventually.
        /// </summary>
        [Obsolete("This method is part of the old consent flow and should no longer be used. Please use StopDataCollection() and/or RequestDataDeletion() instead as appropriate. For more information, please see the migration guide: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide")]
        void OptOut();

        /// <summary>
        /// Disables data collection, preventing any further events from being recorded or uploaded.
        /// A final upload containing any events that are currently buffered will be attempted.
        ///
        /// Data collection can be re-enabled later, by calling the StartDataCollection method.
        /// </summary>
        void StopDataCollection();

        /// <summary>
        /// Requests that all historic data for this user be purged from the back-end and disables data collection.
        /// This can be called regardless of whether data collection is currently enabled or disabled.
        ///
        /// If the purge request fails (e.g. due to the client being offline), it will be retried until it is successful, even
        /// across multiple sessions if necessary.
        /// </summary>
        void RequestDataDeletion();

        /// <summary>
        /// Allows other sources to write events with common analytics parameters to the Analytics service. This is primarily for use
        /// by other packages - as this method adds common parameters that may not be expected in the general case, for custom events
        /// you should use the <c>CustomData</c> method instead.
        /// </summary>
        /// <param name="eventToRecord">Internal event to record</param>
        [Obsolete("This mechanism is no longer supported and will be removed in a future version. Use the new Core IAnalyticsStandardEventComponent API instead.")]
        void RecordInternalEvent(Event eventToRecord);

        /// <summary>
        /// Record an acquisitionSource event, if the player has opted in to data collection (see OptIn method).
        /// </summary>
        /// <param name="acquisitionSourceParameters">(Required) Helper object to handle parameters.</param>
        void AcquisitionSource(AcquisitionSourceParameters acquisitionSourceParameters);

        /// <summary>
        /// Allows you to disable the Analytics service. When the service gets disabled all currently cached data both in RAM and on disk
        /// will be deleted and any new events will be voided. By default the service is enabled so you do not need to call this method on start.
        /// Will return instantly when disabling, must be awaited when re-enabling.
        /// </summary>
        /// <example>
        /// To disable the Analytics Service before the game starts
        /// <code>
        /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// static void DisableAnalytics()
        /// {
        ///     AnalyticsService.Instance.SetAnalyticsEnabled(false);
        /// }
        /// </code>
        /// </example>
        [Obsolete("This method is part of the old consent flow and should no longer be used. For more information, please see the migration guide: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide")]
        Task SetAnalyticsEnabled(bool enabled);

        /// <summary>
        /// Converts an amount of currency to the minor units required for the objects passed to the Transaction method.
        /// This method uses data from ISO 4217. Note that this method expects you to pass in currency in the major units for
        /// conversion - if you already have data in the minor units you don't need to call this method.
        /// For example - 1.99 USD would be converted to 199, 123 JPY would be returned unchanged.
        /// </summary>
        /// <param name="currencyCode">The ISO4217 currency code for the input currency. For example, USD for dollars, or JPY for Japanese Yen</param>
        /// <param name="value">The major unit value of currency, for example 1.99 for 1 dollar 99 cents.</param>
        /// <returns>The minor unit value of the input currency, for example for an input of 1.99 USD 199 would be returned.</returns>
        long ConvertCurrencyToMinorUnits(string currencyCode, double value);

        /// <summary>
        /// Gets the user ID that Analytics is currently recording into the userId field of events.
        /// </summary>
        /// <returns>The user ID as a string</returns>
        string GetAnalyticsUserID();

        /// <summary>
        /// Gets the session ID that is currently recording into the sessionID field of events.
        /// </summary>
        /// <returns>The session ID as a string</returns>
        string SessionID { get; }
    }
}
