using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class CommandSetWriter : IDataConverter<BuildCommandSet, BuildSettings, string, BuildOutput>
    {
        private Dictionary<string, BuildCommandSet.Command> m_NameToBundle = new Dictionary<string, BuildCommandSet.Command>();
        private Dictionary<string, List<string>> m_NameToDependents = new Dictionary<string, List<string>>();

        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(BuildCommandSet.Command command, BuildSettings settings, bool useCache)
        {
            if (!useCache)
                return new Hash128();

            // NOTE: correct hash should be based off command, command dependencies, dependent commands, build target, build group, and build typedb
            // TODO: Remove dependents once Usage Tag calculation is passed into the write command
            var bundles = new List<BuildCommandSet.Command>();
            bundles.Add(command);
            foreach (var dependency in command.assetBundleDependencies)
                bundles.Add(m_NameToBundle[dependency]);

            var dependents = m_NameToDependents[command.assetBundleName];
            foreach (var dependent in dependents)
                bundles.Add(m_NameToBundle[dependent]);

            return HashingMethods.CalculateMD5Hash(Version, bundles, settings.target, settings.group, settings.typeDB);
        }

        public bool Convert(BuildCommandSet commandSet, BuildSettings settings, string outputFolder, out BuildOutput output, bool useCache = true)
        {
            if (useCache)
            {
                // Generate data needed for cache hash generation
                foreach (var command in commandSet.commands)
                {
                    m_NameToBundle[command.assetBundleName] = command;

                    foreach (var dependency in command.assetBundleDependencies)
                    {
                        List<string> dependents;
                        if (!m_NameToDependents.TryGetValue(dependency, out dependents))
                            m_NameToDependents[dependency] = dependents;
                        dependents.Add(command.assetBundleName);
                    }
                }
            }

            var results = new List<BuildOutput.Result>();
            foreach (var command in commandSet.commands)
            {
                BuildOutput result;
                Hash128 hash = CalculateInputHash(command, settings, useCache);
                if (useCache && TryLoadFromCache(hash, outputFolder, out result))
                {
                    results.AddRange(result.results);
                    continue;
                }
                
                result = BuildInterface.WriteResourceFile(commandSet, settings, outputFolder, command.assetBundleName);
                results.AddRange(result.results);

                if (useCache && !TrySaveToCache(hash, result, outputFolder))
                    BuildLogger.LogWarning("Unable to cache CommandSetWriter results for command '{0}'.", command.assetBundleName);
            }

            output = new BuildOutput();
            output.results = results.ToArray();
            return true;
        }

        private bool TryLoadFromCache(Hash128 hash, string outputFolder, out BuildOutput output)
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

        private bool TrySaveToCache(Hash128 hash, BuildOutput output, string outputFolder)
        {
            var artifacts = new List<string>();
            foreach (var result in output.results)
            {
                foreach(var resource in result.resourceFiles)
                    artifacts.Add(Path.GetFileName(resource.fileName));
            }

            return BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts.ToArray(), outputFolder);
        }
    }
}
