using System.IO;
using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ArchiveWriter : IDataConverter<BuildOutput, BuildCompression, string, uint[]>
    {
        public Hash128 CalculateInputHash(BuildOutput commandSet, BuildCompression compression, string outputFolder)
        {
            // TODO: may need to use the resource files as a hash input
            return HashingMethods.CalculateMD5Hash(commandSet, compression);
        }

        public bool Convert(BuildOutput output, BuildCompression compression, string outputFolder, out uint[] crc)
        {
            // TODO: Validate compression settings

            crc = new uint[output.results.Length];
            
            // TODO: Prepare settings.outputFolder
            Directory.CreateDirectory(outputFolder);

            for (var i = 0; i < output.results.Length; i++)
            {
                var filePath = string.Format("{0}/{1}", outputFolder, output.results[i].assetBundleName);
                crc[i] = BuildInterface.ArchiveAndCompress(output.results[i].resourceFiles, filePath, compression);
            }
            return true;
        }

        public bool LoadFromCacheOrConvert(BuildOutput output, BuildCompression compression, string outputFolder, out uint[] crc)
        {
            string rootCachePath;
            string[] artifactPaths;

            var hash = CalculateInputHash(output, compression, outputFolder);
            if (BuildCache.TryLoadCahcedResultsAndArtifacts(hash, out crc, out artifactPaths, out rootCachePath))
            {
                // TODO: Prepare settings.outputFolder
                Directory.CreateDirectory(outputFolder);

                foreach (var artifact in artifactPaths)
                {
                    var file = new FileInfo(artifact);
                    file.CopyTo(artifact.Replace(rootCachePath, outputFolder), true);
                }
                return true;
            }

            if (!Convert(output, compression, outputFolder, out crc))
                return false;
           
            var artifacts = new string[output.results.Length];
            for (var i = 0; i < output.results.Length; i++)
                artifacts[i] = output.results[i].assetBundleName;
            BuildCache.SaveCachedResultsAndArtifacts(hash, crc, artifacts, outputFolder);
            return true;
        }
    }
}
