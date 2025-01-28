using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TikTokLiveUnity.Editor
{
    /// <summary>
    /// Helper-Class for Methods related to the Project-Manifest & Scoped Registries
    /// <para>
    /// Unfortunately, Unity does not expose the methods for listing & adding registries.
    /// Thus, we have to manually read & edit the manifest.json
    /// </para>
    /// </summary>
    internal static class ManifestHelper
    {
        internal class ScopedRegistry
        {
            public string name;
            public string url;
            public string[] scopes;
        }

        internal class ManifestJson
        {
            public Dictionary<string, string> dependencies = new Dictionary<string, string>();

            public List<ScopedRegistry> scopedRegistries = new List<ScopedRegistry>();
        }

        internal static bool HasScopedRegistry(string registryUrl)
        {
            string manifestJson = File.ReadAllText(Path.Combine(Application.dataPath, "..", "Packages/manifest.json"));
            ManifestJson manifest = JsonConvert.DeserializeObject<ManifestJson>(manifestJson);
            return manifest.scopedRegistries.Any(registry => registry.url == registryUrl);
        }

        internal static string[] ListScopesInRegistry(string registryUrl)
        {
            string manifestJson = File.ReadAllText(Path.Combine(Application.dataPath, "..", "Packages/manifest.json"));
            ManifestJson manifest = JsonConvert.DeserializeObject<ManifestJson>(manifestJson);
            if (manifest.scopedRegistries.Any(registry => registry.url == registryUrl))
            {
                ScopedRegistry registry = manifest.scopedRegistries.Find(registry => registry.url == registryUrl);
                return registry.scopes;
            }
            else
            {
                return null;
            }
        }

        internal static void AddScopesToRegistry(string registryUrl, string[] scopes)
        {
            if (!HasScopedRegistry(registryUrl))
            {
                throw new ArgumentException("Cannot add scopes to a registry that doesn't exist.", registryUrl);
            }
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages/manifest.json");
            string manifestJson = File.ReadAllText(manifestPath);
            ManifestJson manifest = JsonConvert.DeserializeObject<ManifestJson>(manifestJson);
            ScopedRegistry registry = manifest.scopedRegistries.Find(registry => registry.url == registryUrl);
            List<string> scopesList = new List<string>(registry.scopes);
            foreach (string scope in scopes)
            {
                if (!scopesList.Contains(scope))
                {
                    scopesList.Add(scope);
                }
            }
            registry.scopes = scopesList.ToArray();
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest));
        }

        internal static void AddRegistry(string registryUrl, string name, string[] scopes)
        {
            if (HasScopedRegistry(registryUrl))
            {
                throw new ArgumentException("Cannot add a registry that already exists.", registryUrl);
            }
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages/manifest.json");
            string manifestJson = File.ReadAllText(manifestPath);
            ManifestJson manifest = JsonConvert.DeserializeObject<ManifestJson>(manifestJson);
            ScopedRegistry registry = new ScopedRegistry
            {
                name = name,
                url = registryUrl,
                scopes = scopes
            };
            manifest.scopedRegistries.Add(registry);
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest));
        }
    }
}