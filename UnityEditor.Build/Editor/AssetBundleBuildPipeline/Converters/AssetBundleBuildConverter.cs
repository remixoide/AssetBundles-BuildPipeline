using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class AssetBundleBuildConverter : IDataConverter<AssetBundleBuild[], BuildInput>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(AssetBundleBuild[] input, bool useCache)
        {
            if (!useCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(Version, input);
        }

        public bool Convert(AssetBundleBuild[] input, out BuildInput output, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(input, useCache);
            if (useCache && BuildCache.TryLoadCachedResults(hash, out output))
                return true;

            // Convert inputs
            output = new BuildInput();

            if (input.IsNullOrEmpty())
            {
                BuildLogger.LogError("Unable to continue packing. Input is null or empty!");
                return false;
            }

            output.definitions = new BuildInput.Definition[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                output.definitions[i].assetBundleName = input[i].assetBundleName;
                output.definitions[i].explicitAssets = new BuildInput.AddressableAsset[input[i].assetNames.Length];
                for (var j = 0; j < input.Length; j++)
                {
                    var path = AssetDatabase.AssetPathToGUID(input[i].assetNames[j]);
                    output.definitions[i].explicitAssets[j].asset = new GUID(path);
                    if (input[i].addressableNames.IsNullOrEmpty() || input[i].addressableNames.Length <= j || string.IsNullOrEmpty(input[i].addressableNames[j]))
                        output.definitions[i].explicitAssets[j].address = path;
                    else
                        output.definitions[i].explicitAssets[j].address = input[i].addressableNames[j];
                }
            }

            // Cache results
            if (useCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache AssetBundleBuildConverter results.");
            return true;
        }
    }
}
