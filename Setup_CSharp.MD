# TikTokLiveSharp v1.2.0

## Downloading the Source Code
Download the TikTokLiveSharp-SourceCode from the [Releases-Page](https://github.com/frankvHoof93/TikTokLiveSharp/releases/)

## Downloading directly from GitHub-Source
When downloading the main-branch, the folder will contain both TikTokLiveSharp and TikTokLiveUnity-assets.
To resolve this, combine [Base.gitignore](Base.gitignore) & [TikTokLiveSharp.gitignore](TikTokLiveSharp.gitignore) into a new [.gitignore](.gitignore).
Then overwrite the existing gitignore with this file.
You can delete all files then discard that deletion to ensure no TikTokLiveUnity-assets remain.

## Nuget-Package
TikTokLiveSharp is now also available as a Nuget-Package under the name TikTokLive_Sharp.

## Building the Project
Open [The Project](TikTokLiveSharp/TikTokLiveSharp.sln) in Visual Studio to build the project.
The library should build without errors.

## Running the Test-Project
With [The Project](TikTokLiveSharp/TikTokLiveSharp.sln) opened & built in Visual Studio, set [TikTokLiveSharp_TestApplication](TikTokLiveSharp_TestApplication/TikTokLiveSharpTestApplication.csproj) as the Startup-Project and Start the Project.
Enter a HostID to display incoming messages in the Console.
