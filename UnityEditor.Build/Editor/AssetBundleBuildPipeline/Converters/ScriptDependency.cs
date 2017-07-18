using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ScriptDependency : IDataConverter<ScriptCompilationSettings, string, ScriptCompilationResult>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(ScriptCompilationSettings settings, bool useCache)
        {
            if (!useCache)
                return new Hash128();

            // TODO: Figure out a way to cache script compiling
            return new Hash128();
        }

        public bool Convert(ScriptCompilationSettings settings, string outputFolder, out ScriptCompilationResult output, bool useCache = true)
        {
            // TODO: Figure out a way to cache script compiling
            useCache = false;

            Hash128 hash = CalculateInputHash(settings, useCache);
            if (useCache && TryLoadFromCache(hash, outputFolder, out output))
                return true;

            output = PlayerBuildInterface.CompilePlayerScripts(settings, outputFolder);

            if (useCache && !TrySaveToCache(hash, output, outputFolder))
                BuildLogger.LogWarning("Unable to cache ScriptDependency results.");
            return true;
        }

        private bool TryLoadFromCache(Hash128 hash, string outputFolder, out ScriptCompilationResult output)
        {
            string rootCachePath;
            string[] artifactPaths;

            if (!BuildCache.TryLoadCachedResultsAndArtifacts(hash, out output, out artifactPaths, out rootCachePath))
                return false;

            Directory.CreateDirectory(outputFolder);

            foreach (var artifact in artifactPaths)
                File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
            return true;
        }

        private bool TrySaveToCache(Hash128 hash, ScriptCompilationResult output, string outputFolder)
        {
            var artifacts = new string[output.assemblies.Length];
            for (var i = 0; i < output.assemblies.Length; i++)
                artifacts[i] = output.assemblies[i];

            return BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts, outputFolder);
        }
    }
}
