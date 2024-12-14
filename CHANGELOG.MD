# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - Fix empty json data for live info - 2024-12-14

Bugfix for errors related to a change Tiktok made related to headers.

### Fixed

- Removed version_code header
- Changed UserAgent (added Mozilla)

## [1.2.0] - Fix 403 Errors on Gifts & RoomInfo - 2024-07-14

Bugfix for 403-Errors on fetching Available Gifts & RoomInfo.

### Added

- Updated Logging to direct users to Discord when issues arise with the signing-server.
- Added HideInCallstack-Attribute to Logging-methods to hide these methods in Unity's Console-UI.
  Note: This only works if the user enables "Strip logging callstack" in the console's menu.

### Fixed

- Fixed (secondary) regex-match for RoomId
- Removed Origin-Header from HTTP-Requests, as that seemed to be causing the 403-errors.

### Changed

- Replaced ?? when checking cancellationtoken with HasValue to try to prevent 'Nullable object must have a value'.
- Updated BugReport-Template to include Environment-info.

## [1.1.1] - Websocket Bugfix

Bugfix for failing Websocket-Connection

### Added

- RunAsync for TTLiveSharp, for more control over Threads
- Settings for Custom Signing-Server & Signing-ApiKey
- Additional StatusCode-Messaging for Signing-Server

### Fixed

- Fixed incorrect Header(s) on WebSocket-Client
- Fixed AlreadyConnectedException when retrying a Connect()

### Changed

- Replaced HTTPUtility with WebUtility for URL-Encoding

## [1.1.0] - 

Bugfix for failing/closing Websocket-Connections

### Added

- Added ControlAction-Enum
- Added OnLiveResumed-Event
- Added Reconnect-Timeout used when an exception occurs whilst connecting
- Added full log of (outer) message when logging unparsed data

### Fixed

- Removed br from accepted encodings
- Fixed an issue with Websocket-Headers & -Parameters causing the SocketConnection to close instantly.

### Changed

- Changed Signing to use new Fetch-Endpoint
- Changed some parameters in the Unity-SampleScene to reduce overhead for default users.
- Removed unneccesary memory allocations in Dispatcher-script


## [1.0.0] - 2023-10-09

Initial full release of TikTokLiveSharp/TikTokLiveUnity

### Note
THIS IS A BREAKING UPDATE FOR USERS OF PREVIOUS VERSIONS OF THE LIBRARY.
PROCEED IN UPDATING WITH CAUTION!

Between the previous version to v1.0.0 a complete re-write was performed of
the schema used when decoding messages from the TikTokLive-Server.
This means that many Events & their underlying paramaters will have changed.
When updating from previous versions to v1.0.0, you will need to re-write your
code that hooks to these events.

### Added

- Added DisplayedGifts, which filters AvailableGifts down to those displayed for the room
- Added the ability to connect by RoomId instead of HostId
- Added the ability to skip checking the RoomInfo whilst connecting
- Added usage of Custom Parameters for clients & Requests

### Fixed

- Proxy is now properly used for both HTTP- and Websocket-Connection
- Lib now works properly on UWP-backend
- Enabled Images in Example

### Changed

- Complete rewrite of TikTokLive-BackendData & Events


## [0.9.5] - 2023-06-11

### Added

- Added checking for socket-state and recreation of SocketClient if it was aborted (by server?)

### Fixed

### Changed

- Added Gift-Name to example-code

### Removed

- Removed Icon (in TTLUnity) only used for NuGet-Package

## [0.9.4] - 2023-05-20

### Added

- Added Static GetUserExists & GetUserStreaming-Methods that can be used to check if a user-profile exists and/or is livestreaming
- Added newly found Message-Data (recently added by ByteDance)

### Fixed

- Fixed Message-Data not being logged alongside (some) exceptions

### Changed

- Updated ProtoSchema to reflect updates in messaging

### Removed


## [0.9.3] - 2023-04-29

### Added

- Added newly found Message-Data (recently added by ByteDance)

### Fixed

### Changed

- Changed Message-Data in (Hourly)RankMessages (recently changed by ByteDance)

### Removed

- Disabled downloading of Textures/Sprites in Example-Code for TikTokLiveUnity, as it seems to be leading to random crashes somehow.

## [0.9.2] - 2023-04-25

### Added

- Added newly found Message-Data (recently added by ByteDance)

### Fixed

- Fixed bugs in Disconnect causing disconnect to throw an exception.

### Changed

- Set Assemblies to be auto-referenced, so users do not need their own AssemblyDef to use the project

### Removed

## [0.9.1] - 2023-03-31

### Added

- Added newly found Message-Data (recently added by ByteDance)

### Fixed

- Added overly cautious Null-Checking in Model. Instead of Nullref, data will now simply default to NULL if not available
- Fixed invalid DataType in WebcastRoomPinMessage (Pos 35)

### Changed

- Updated Model based on Protobuf
- Buffer-array for messages is now re-used to minimize garbage
- Connecting-Boolean is now set to False once fully connected

### Removed

- Removed Dispatching for Debug-Logs in Unity, as it is not required and only adds overhead.


## [0.9.0] - 2023-03-11

### Added

- Initial release of new Fork for this Project. Consists of many breaking changes with previous version.

### Fixed

### Changed

### Removed
