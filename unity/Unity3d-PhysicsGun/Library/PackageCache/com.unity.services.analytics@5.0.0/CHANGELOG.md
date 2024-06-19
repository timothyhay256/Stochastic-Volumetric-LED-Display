# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [5.0.0] - 2023-06-29

### Added

* New initialization and consent flow. Please see the migration guide for more information: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide

### Deprecated

* The old initialization and consent flow should no longer be used and will be removed in a future version of the SDK. Please see the migration guide for more information: https://docs.unity.com/analytics/en/manual/AnalyticsSDK5MigrationGuide

### Changed

* Updated `com.unity.services.core` dependency to 1.10.1

### Breaking Changes
* The package no longer has a dependency on Newtonsoft.Json
* The package now has a dependency on the Unity JsonSerialize module (JsonUtility)
* A number of elements that were previously marked as Obsolete have now been removed

### Fixed

* Analytics no longer blocks services initialization (`UnityServices.InitializeAsync`) if there is no internet connection
* It is now possible to opt in to data collection during a session where the player has previously opted out (requires migration to the new consent flow)
* Application.persistentDataPath is no longer requested on some platforms where access to the file system is denied by default
* Event buffer is now cleared on a wider variety of server responses
* Events are more eagerly cached to disk (if available) on shutdown to prevent data loss if game is closed while offline
* Data deletion requests are now sent using the custom user ID if one is set, instead of always using the installation ID

## [4.4.2] - 2023-04-04

### Fixed

* Revoking consent (opting out) now successfully uploads a data deletion request

### Improved

* Recording events is now significantly faster and generates fewer memory allocations

## [4.4.1] - 2023-03-15

### Fixed

* Changing ExternalUserId now takes effect immediately, rather than after an unpredictable delay

## [4.4.0] - 2023-03-02

### Added

* CustomData method now supports IDictionary<string,object>, IList<object> and Enum parameters

### Changed

* Events are now serialised immediately when recorded rather than as part of the upload process. This should alleviate any hitches that might have been experienced during upload (every 60 seconds)
* Updated `com.unity.services.core` dependency to 1.8.1

### Fixed

* Custom user ID can now be changed at runtime (by updating `UnityServices.ExternalUserId`)
* Recording a single event that is too big to upload (over 4MB) no longer prevents any further events from being uploaded (event is immediately discarded with a warning)
* Session ID is now refreshed when application is paused for over 5 minutes (when Run In Background is false)

### Deprecated

* The `RecordEvent(Event event)` API is no longer supported and will be removed in a future version

## [4.3.0] - 2022-10-25

### Added

* Added CustomData(string eventName) method for recording events that do not have any parameters

### Fixed

* Issue where a single corrupt event could prevent all subsequent events from being sent
* NullReferenceException when passing null instead of a Dictionary of parameters to CustomData for an event that does not have any parameters
* NullReferenceException when passing null for the currency code to ConvertCurrencyToMinorUnits; it now throws an ArgumentNullException if the currency code is either null or empty
* Compiler error on 2020.1 editor versions
* Documentation comment on IAnalyticsService.Flush method to clarifybehaviour and usage

### Changed

* AnalyticsContainer object is no longer created automatically on start-up; it is now spawned during UnityServices.InitializeAsync

## [4.2.0] - 2022-08-15

### Added

* SessionID property that returns the GUID value currently being used to populate the "sessionID" parameter of all events

### Changed

* Reduced frequency of gameRunning event to reduce excess traffic (this will not affect the quality of your data)

### Fixed

* SDK initialisation failing silently on WebGL due to problem with privacy consent flow
* SDK event batching for upload failing silently on WebGL

## [4.1.0] - 2022-06-16

### New

* Added a method to access the user ID used by Analytics at runtime

### Fixed

* Events are now recorded with timestamps including milliseconds
* XML documentation is available for more model objects
* A better error message will be displayed when the project ID is missing

## [4.0.1] - 2022-05-18

### Fixed

* Change assertions to error logs for missing required parameters

## [4.0.0] - 2022-05-12

The UGS Analytics is no longer pre-release! No other changes in this version.

## [4.0.0-pre.3] - 2022-05-10

### Fixed

* Events will now consistently use local time + offset for timestamps
* Fixes an issue that could occur when the SDK was disabled

## [4.0.0-pre.2] - 2022-04-06

### New

* Added a new event (`transactionFailed`) for recording failed transactions

### Changed

* IDFA usage has also been removed from the SDK - this identifier will no longer be added to events automatically when available.
* This release restores the previous `Events` API for backwards compatability with `3.0.0` versions of the SDK. This API will be removed in a future release.

## [4.0.0-pre.1] - 2022-03-14

### Breaking Changes

* The API of the Analytics package has been updated to match the other UGS packages. This means that APIs for recording events that were previously available on the `Events` static class are now available via `AnalyticsService.Instance` instead. 
In addition, some classes that were previously nested in other types have been moved to standalone classes.
  * The `Events` static class has changed to `AnalyticsService.Instance` - the same event recording methods are found on this new instance
  * The `Transaction` method now uses standalone classes for `Product`, `TransactionType`, etc.
  * The `AdImpressionArgs` object has been changed to an `AdImpressionParameters` struct
  * Some parameter objects have been changed from lowercase fields to uppercase to match C# guidelines
* Code in the `Unity.Services.Analytics.Editor.Settings` namespace has been made internal as it was never meant to be public.

### New Features

* Added support for sending a new event: acquisitionSource.
* Added a method to convert currency to units suitable for the Transaction event
* Added new Sample Scene
* Added abilitiy to disable and re-enable the Analytics SDK

### Bug Fixes

* Fixed a bug that would block the main thread when trying to send large amounts of events

## [3.0.0-pre.4] - 2022-02-15

* Fixed a bug where event data was not cached locally when the game closes
* Fixed a bug where floats were not serialized properly in cultures where the `,` character is used for decimals rather than `.`

## [3.0.0-pre.3] - 2022-01-27

* Adds support for using a custom analytics ID via the Core SDK.

## [3.0.0-pre.2] - 2021-12-02

* Analytics Runtime dependency has been updated, the PIPL headers are now included in `ForgetMe` event, when appropriate.

## [3.0.0-pre.1] - 2021-11-26

### Added

**Breaking Change**: 
- New APIs provided for checking if PIPL consent is needed, and recording users' consent. 
  It is now required to check if any consent is required, and provide that consent if necessary, before the events will be sent from the SDK.

## [2.0.7-pre.7] - 2021-10-20

### Added

* projectID parameter to all events

### Fixed

* GameStart event `idLocalProject` having a nonsense value
* Heartbeat cadence being affected by Time Scale
* Failing to compile for WebGL with error " The type or namespace name 'DllImportAttribute' could not be found"

### Changed

* User opt-out of data collection. Developers must expose this mechanism to users in an appropriate way:
  * Give users access to the privacy policy, the URL for which is stored in the `Events.PrivacyUrl` property
  * Disable analytics if requested using the `Events.OptOut()` method

### Removed

* Deprecated Transaction event `isInitiator` parameter
* Deprecated previous opt-out mechanism (DataPrivacy and DataPrivacyButton)

## [2.0.7-pre.6] - 2021-08-26

### Fixed

* GameRunning event being recorded and uploaded erratically
* Removed some obsolete steps from readme
* Clarified and added some missing XmlDoc comments on public methods

## [2.0.7-pre.4] - 2021-08-19

### Changed

* Updated README
* Regenerated `.meta` files for privacy

## [2.0.7-pre.2] - 2021-08-18

### Removed

* Version of CustomData method that takes an Event Version

### Changed

* Regen'd `.meta` files for privacy

### Added

* Added UI as a dependency

## [2.0.7] - 2021-08-09

### Changed

* New custom code entry point.
* Arguments for AdImpression now handled by an object.

### Added

* New way to interact with buffer.

## [2.0.6] - 2021-06-17

### Changed

* Bump dependencies

## [2.0.5] - 2021-05-18

### Changed

* Use Core for Authentication ID
* Use Core for Install ID
* Use `https` instead of `http`

## [2.0.4] - 2021-05-10

### Changed

* URL now uses the new collect url based off project_id and not a legacy one.

### Removed

* UI for setting up the collect url.

## [2.0.3] - 2021-05-05

### Added

* Re added support for 2019.4
* Update dependencies

## [2.0.2] - 2021-04-29

### Added

* Project settings UI

### Removed

 * `Setup()` API entry point
 * Custom UserID and SessionID

## [0.1.1] - 2021-04-01

### Changed

* Removed util package
* Changed `RecordEvent` entry point to `CustomData`

## [0.1.0] - 2021-03-31

### Added

* Standard events
