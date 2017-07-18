using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

using SceneResourceMap = System.Collections.Generic.Dictionary<UnityEditor.GUID, UnityEditor.Experimental.Build.AssetBundle.ResourceFile[]>;
using CRCMap = System.Collections.Generic.Dictionary<string, uint>;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ResourceFileArchiver : IDataConverter<BuildOutput, SceneResourceMap, BuildCompression, string, CRCMap>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(List<ResourceFile> resourceFiles, BuildCompression compression, bool useCache)
        {
            if (!useCache)
                return new Hash128();

            var fileHashes = new List<Hash128>();
            foreach (var file in resourceFiles)
                fileHashes.Add(HashingMethods.CalculateFileMD5Hash(file.fileName));
            return HashingMethods.CalculateMD5Hash(Version, fileHashes, compression);
        }

        public bool Convert(BuildOutput writenData, SceneResourceMap sceneResources, BuildCompression compression, string outputFolder, out CRCMap output, bool useCache = true)
        {
            output = new CRCMap();

            foreach (var bundle in writenData.results)
            {
                var resourceFiles = new List<ResourceFile>(bundle.resourceFiles);
                foreach (var asset in bundle.assetBundleAssets)
                {
                    ResourceFile[] sceneFiles;
                    if (!sceneResources.TryGetValue(asset, out sceneFiles))
                        continue;
                    resourceFiles.AddRange(sceneFiles);
                }

                uint crc;
                Hash128 hash = CalculateInputHash(resourceFiles, compression, useCache);
                if (useCache && TryLoadFromCache(hash, outputFolder, out crc))
                    continue;

                var filePath = string.Format("{0}/{1}", outputFolder, bundle.assetBundleName);
                crc = BuildInterface.ArchiveAndCompress(resourceFiles.ToArray(), filePath, compression);
                output[bundle.assetBundleName] = crc;

                if (useCache && !TrySaveToCache(hash, filePath, crc, outputFolder))
                    BuildLogger.LogWarning("Unable to cache ResourceFileArchiver result for bundle {0}.", bundle.assetBundleName);
            }

            
            return true;
        }

        private bool TryLoadFromCache(Hash128 hash, string outputFolder, out uint output)
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

        private bool TrySaveToCache(Hash128 hash, string filePath, uint output, string outputFolder)
        {
            var artifacts = new List<string>();
            artifacts.Add(filePath);

            return BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts.ToArray(), outputFolder);
        }
    }
}
