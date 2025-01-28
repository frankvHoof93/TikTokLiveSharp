using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace TikTokLiveUnity.Editor
{
    /// <summary>
    /// Installs WebP-Library to allow for download of WebP-Images as Texture2D
    /// <para>
    /// Library can be found at https://github.com/netpyoung/unity.webp and is used under its BSD-License
    /// </para>
    /// </summary>
    public static class WebPInstaller
    {
        /// <summary>
        /// GitHub-Url for WebP-Library
        /// </summary>
        private const string WEBP_URL = @"https://github.com/netpyoung/unity.webp.git?path=unity_project/Assets/unity.webp";
        /// <summary>
        /// Name of Unity-Package
        /// </summary>
        private const string WEBP_NAME = "Unity.WebP";
        /// <summary>
        /// URL for OpenUMP-Registry
        /// </summary>
        private const string OPENUPM_REGISTRY_URL = @"https://package.openupm.com";
        /// <summary>
        /// Scopes required in OpenUPM for WebP-Library
        /// </summary>
        private static readonly string[] OPENUPM_SCOPES = new[] { "com.netpyoung.webp", "org.nuget.system.runtime.compilerservices.unsafe" };

        /// <summary>
        /// Active PackageManager-Request
        /// </summary>
        private static Request PackageManagerRequest = null;

        /// <summary>
        /// Checks for & Installs WebP-Package
        /// <para>
        /// Adds Button to Top-Menu in Unity Editor
        /// </para>
        /// </summary>
        [MenuItem("TikTokLive/Setup/Install WebP Support")]
        private static void InstallWebP()
        {
            if (PackageManagerRequest != null)
                return; // Already Processing a Request
            Debug.Log("[TikTokLive] Attempting to install WebP-Support...");
            ResolveScopedRegistry();
            PackageManagerRequest = Client.List(); // List Installed Packages
            EditorApplication.update += CheckProgress; // Add callback to Editor-UpdateLoop
        }

        /// <summary>
        /// Checks if required OpenUMP-Registry is available with correct Scopes for WebP.
        /// <para>
        /// Will add registry or scopes if this is needed.
        /// </para>
        /// </summary>
        private static void ResolveScopedRegistry()
        {
            if (!ManifestHelper.HasScopedRegistry(OPENUPM_REGISTRY_URL))
            {
                // Add Registry
                Debug.Log("[TikTokLive] Adding OpenUPM-Registry.");
                ManifestHelper.AddRegistry(OPENUPM_REGISTRY_URL, "OpenUPM", OPENUPM_SCOPES);
                // Force Resolve to update.
                Client.Resolve();
            }
            else
            {
                // Check Scopes in Registry
                string[] scopes = ManifestHelper.ListScopesInRegistry(OPENUPM_REGISTRY_URL);
                List<string> scopesToAdd = new List<string>(2);
                foreach (string scope in OPENUPM_SCOPES)
                {
                    if (!scopes.Contains(scope))
                    {
                        scopesToAdd.Add(scope);
                    }
                }
                if (scopesToAdd.Count > 0)
                {
                    // Add Scopes
                    Debug.Log("[TikTokLive] Adding required Scopes to OpenUPM-Registry.");
                    ManifestHelper.AddScopesToRegistry(OPENUPM_REGISTRY_URL, scopesToAdd.ToArray());
                    // Force Resolve to update.
                    Client.Resolve();
                }
            }
        }

        /// <summary>
        /// Callback that checks progress on Active PackageManagerRequest
        /// </summary>
        private static void CheckProgress()
        {
            if (PackageManagerRequest.IsCompleted)
            {
                if (PackageManagerRequest.Status != StatusCode.Success)
                {
                    Debug.LogError($"[TikTokLive] Error while Installing WebP Package: {PackageManagerRequest.Error.message}");
                    EditorApplication.update -= CheckProgress;
                }
                else
                {
                    if (PackageManagerRequest as ListRequest != null) // List Loaded, see if WebP has been installed
                    {
                        ListRequest listReq = (ListRequest)PackageManagerRequest;
                        foreach (var pkg in listReq.Result)
                            if (pkg.displayName.Equals(WEBP_NAME))
                            {
                                Debug.Log("[TikTokLive] WebP already installed!");
                                EditorApplication.update -= CheckProgress; // Remove self from Update-Loop
                                PackageManagerRequest = null;
                                return;
                            }
                        // No WebP Installed. Add Now
                        Debug.Log($"[TikTokLive] Installing WebP-Package. Please wait.{System.Environment.NewLine}If this operation also affected the OpenUPM-Registry, please close any related popup and note that this operation can take a bit longer to complete.");
                        PackageManagerRequest = UnityEditor.PackageManager.Client.Add(WEBP_URL);
                        return;
                    }
                    else if (PackageManagerRequest as AddRequest != null) // Install of WebP Finished
                    {
                        Debug.Log("[TikTokLive] Installed WebP-Support");
                        EditorApplication.update -= CheckProgress; // Remove self from Update-Loop
                        PackageManagerRequest = null;
                    }
                    else
                    {
                        Debug.LogError("[TikTokLive] Error while parsing PackageManager-Request. Stopping.");
                        EditorApplication.update -= CheckProgress; // Remove self from Update-Loop
                    }
                }
                PackageManagerRequest = null; // Void this Completed Request
            }
        }
    }
}