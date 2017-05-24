using System.IO;
using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ArchiveWriter : IDataConverter<BuildOutput, BuildCompression, string, uint[]>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(BuildOutput output, BuildCompression compression)
        {
            // TODO: may need to use the resource files as a hash input
            return HashingMethods.CalculateMD5Hash(Version, output, compression);
        }

        public bool Convert(BuildOutput output, BuildCompression compression, string outputFolder, out uint[] crc, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(output, compression);
            if (useCache && LoadFromCache(hash, outputFolder, out crc))
                return true;
            
            // Convert inputs
            // TODO: Validate compression settings

            crc = new uint[output.results.Length];
            
            // TODO: Prepare settings.outputFolder
            Directory.CreateDirectory(outputFolder);

            for (var i = 0; i < output.results.Length; i++)
            {
                var filePath = string.Format("{0}/{1}", outputFolder, output.results[i].assetBundleName);
                var dir = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(dir);

                crc[i] = BuildInterface.ArchiveAndCompress(output.results[i].resourceFiles, filePath, compression);
            }
            
            // Cache results
            if (useCache)
                SaveToCache(hash, output, crc, outputFolder);
            return true;
        }

        private bool LoadFromCache(Hash128 hash, string outputFolder, out uint[] crc)
        {
            string rootCachePath;
            string[] artifactPaths;
            
            if (BuildCache.TryLoadCachedResultsAndArtifacts(hash, out crc, out artifactPaths, out rootCachePath))
            {
                // TODO: Prepare settings.outputFolder
                Directory.CreateDirectory(outputFolder);

                foreach (var artifact in artifactPaths)
                    File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
                return true;
            }
            return false;
        }

        private void SaveToCache(Hash128 hash, BuildOutput output, uint[] crc, string outputFolder)
        {
            var artifacts = new string[output.results.Length];
            for (var i = 0; i < output.results.Length; i++)
                artifacts[i] = output.results[i].assetBundleName;
            BuildCache.SaveCachedResultsAndArtifacts(hash, crc, artifacts, outputFolder);
        }
    }
}
