using System.Text;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class PrepareScene : IDataConverter<string, BuildSettings, SceneLoadInfo>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(string scenePath, BuildSettings settings)
        {
            // TODO: get scene asset hash
            return HashingMethods.CalculateMD5Hash(Version, scenePath, settings);
        }

        public bool Convert(string scenePath, BuildSettings settings, out SceneLoadInfo output, bool useCache = false)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(scenePath, settings);
            if (useCache && LoadFromCache(hash, settings.outputFolder, out output))
                return true;

            output = BuildInterface.PrepareScene(scenePath, settings);

            var msg = new StringBuilder();
            msg.AppendFormat("Scene: '{0}'\n", output.scene);
            msg.AppendFormat("Processed Scene: '{0}'\n", output.processedScene);
            msg.Append("Referenced Objects:\n");
            if (!output.referencedObjects.IsNullOrEmpty())
            {
                foreach (var objID in output.referencedObjects)
                msg.AppendFormat("\t{0}\n", objID);
            }
            BuildLogger.Log(msg);

            // Cache results
            if (useCache)
                SaveToCache(hash, output, settings.outputFolder);
            // TODO: Change this return based on if WriteResourceFiles was successful or not - Need public BuildReporting
            return true;
        }

        private bool LoadFromCache(Hash128 hash, string outputFolder, out SceneLoadInfo output)
        {
            //string rootCachePath;
            //string[] artifactPaths;

            //if (BuildCache.TryLoadCachedResultsAndArtifacts(hash, out output, out artifactPaths, out rootCachePath))
            //{
            //    // TODO: Prepare settings.outputFolder
            //    Directory.CreateDirectory(outputFolder);

            //    foreach (var artifact in artifactPaths)
            //        File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
            //    return true;
            //}
            output = new SceneLoadInfo();
            return false;
        }

        private void SaveToCache(Hash128 hash, SceneLoadInfo output, string outputFolder)
        {
            //var artifacts = new List<string>();
            //for (var i = 0; i < output.results.Length; i++)
            //{
            //    for (var j = 0; j < output.results[i].resourceFiles.Length; j++)
            //        artifacts.Add(Path.GetFileName(output.results[i].resourceFiles[j].fileName));
            //}
            //BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts.ToArray(), outputFolder);
        }
    }
}
