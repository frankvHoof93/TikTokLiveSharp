# TikTokLiveUnity v1.2.0

## Installing the Project in Unity
- Open the Package Manager Window **(Window -> Package Manager)** 
- Add a Package via Git URL **(+ -> Add package from git URL)**
- Use the following URL: https://github.com/frankvHoof93/TikTokLiveSharp.git#TikTokLiveUnity

## WebP-Support
Some of the Images supplied by TikTokLive are in the WebP-format. This format is not supported by Unity by default.
A library can be added to the project to resolve this. Click **(TikTokLive -> Setup -> Install WebP-Support)** in the Menu of the Unity-Editor after importing the library to automatically add this package to your project.

The [WebP-Library](https://github.com/netpyoung/unity.webp) is provided AS-IS with no warranty.

## Samples
An example showing the library in action is available.
If the library has been installed via the package manager, you can import this example into your project by selecting the Package in the Package Manager Window and clicking "Import" next to the **TikTokLive Example**

## Downloading the Source Code
Download the TikTokLiveUnity-SourceCode from the [Releases-Page](https://github.com/frankvHoof93/TikTokLiveSharp/releases/)

## Downloading directly from GitHub-Source
When downloading the main-branch, the folder will contain both TikTokLiveSharp and TikTokLiveUnity-assets.
To resolve this, combine [Base.gitignore](Base.gitignore) & [TikTokLiveUnity.gitignore](TikTokLiveUnity.gitignore) into a new [.gitignore](.gitignore).
Then overwrite the existing gitignore with this file.
You can delete all files then discard that deletion to ensure no TikTokLiveSharp-assets remain.
