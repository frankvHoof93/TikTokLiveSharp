# TikTokLiveSharp / TikTokLiveUnity v1.2.1

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white&style=flat-square)](https://www.linkedin.com/in/frankvhoof93/)
![Issues](https://img.shields.io/github/issues/frankvHoof93/TikTokLiveSharp)
![Forks](https://img.shields.io/github/forks/frankvHoof93/TikTokLiveSharp)
![Stars](https://img.shields.io/github/stars/frankvHoof93/TikTokLiveSharp)
![Nuget](https://img.shields.io/nuget/dt/TikTokLive_Sharp?logo=nuget)
[![Support Server](https://img.shields.io/discord/977648006063091742.svg?color=7289da&logo=discord&style=flat-square)](https://discord.gg/e2XwPNTBBr)

#### ***This is not an official library nor associated with TikTok or ByteDance in any way***

### Respond to TikTok Live comments, gifts, follows, etc.

## Issues
If you want to showcase a project you've made with this library, create an Issue with the **Showcase** label.
Other than that issues are purely for bugs, if you're having an implementation issue, please send a message in the [discord server](https://discord.gg/e2XwPNTBBr).

## New Fork
This project is a continuation of the original TikTokLiveSharp-Library by sebheron. The original project can be found on [Github](https://github.com/sebheron/TikTokLiveSharp). The Readme for that project can also be found [here](README_ORIGINAL.MD).

As the original Project did not seem to be receiving any new updates, and all the existing TikTokLive-Libraries were lacking in their Messaging-Schema's, this fork involved a re-write of large portions of the original. The original Messaging-Schema has been completely replaced, which can lead to breaking changes. Upgrade to this Library at your own discretion.

## Details
A C# port of the TikTok Live connector library.
Other available versions include:

- [TikTok-Live-Connector](https://github.com/zerodytrash/TikTok-Live-Connector) - Node.js Library (by zerodytrash)
- [GoTikTokLive](https://github.com/Davincible/gotiktoklive) - Go Library (by David Brouwer)
- [TikTokLive](https://github.com/isaackogan/TikTokLive) - Python Library (by Isaac Kogan)
- [TikTokLiveJava](https://github.com/jwdeveloper/TikTokLiveJava) - Java Library (by JW)
- [TikTokLiveRust](https://github.com/jwdeveloper/TikTokLiveRust) - Rust Library (by JW)

The primary incentive behind this library was to update the existing TikTokLive-Library with anew Messaging-Schema and additional Exception-Handling. It's implemented in .NET Standard and should work universally across all .NET supported platforms. An older version of Protobuf-net was used to ensure Unity compatibility.

## Setup
### TikTokLiveSharp
Setup of the C-Sharp Library can be found [here](Setup_CSharp.MD)

### TikTokLiveUnity
Setup of the Unity Library can be found [here](Setup_Unity.MD)

## Contributors

* **Frank van Hoof** - *Creator & Maintainer of TikTokLiveUnity* - [Frank van Hoof](https://github.com/frankvHoof93)
* **Sebheron** - *Original Creator of TikTokLiveSharp* - [sebheron](https://github.com/sebheron)
* **Isaac Kogan** - *Creator of TikTokLive & Signing-Server* - [isaackogan](https://github.com/isaackogan)
* **Zerody** - *Initial Reverse-Engineering Protobuf & Support* - [Zerody](https://github.com/zerodytrash/)
* **David Brouwer** - *Reverse-Engineering Stream Downloads*  - [davincible](https://github.com/davincible)
* **JW** - *Creator of TikTokLiveJava* - [JW](https://github.com/jwdeveloper)
* **David Teather** - *TikTokLive Introduction Tutorial* - [davidteather](https://github.com/davidteather)

## Contributing
### How can I help?
As the TikTokLive-Libraries all use reverse engineering to create their own Protobuf-Schema, much is still unknown about many of the Event-Types and Data-Values. If you think you can contribute, please join the Discord-server or create an issue.

### Do you have a donation-link?
Donations to me personally can be done via [BuyMeACoffee](https://www.buymeacoffee.com/frankvanhoof)

### Downloading the Source
This repo is set up in a way so that both TikTokLiveSharp and TikTokLiveUnity can use the exact same main repo. It uses GitIgnore-files in order to separate out these files to the correct subset for each version.
To reduce main to the subset for your version, combine the [Base.gitignore](Base,gitignore) with the file for the Library (either [TikTokLiveSharp](TikTokLiveSharp.gitignore) or [TikTokLiveUnity](TikTokLiveUnity.gitignore) and apply the result to the base [GitIgnore](.gitignore).
If any conflicts remain after creating the gitignore, delete everything in the Repo-Folder and discard that deletion.
