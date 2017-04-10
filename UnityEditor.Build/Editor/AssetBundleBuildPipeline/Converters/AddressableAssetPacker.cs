using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class AddressableAssetPacker : IDataConverter<BuildInput.AddressableAsset[], BuildInput>
    {
        public uint Version { get { return 1; } }

        private Hash128 CalculateInputHash(BuildInput.AddressableAsset[] input)
        {
            return HashingMethods.CalculateMD5Hash(Version, input);
        }

        public bool Convert(BuildInput.AddressableAsset[] input, out BuildInput output, bool useCache = true)
        {
            // If enabled, try loading from cache
            var hash = CalculateInputHash(input);
            if (useCache && LoadFromCache(hash, out output))
                return true;
            
            // Convert inputs
            output = new BuildInput();

            if (input.IsNullOrEmpty())
            {
                BuildLogger.LogError("Unable to continue packing. Input is null or empty!");
                return false;
            }

            output.definitions = new BuildInput.Definition[input.Length];
            for (var index = 0; index < input.Length; index++)
            {
                output.definitions[index].assetBundleName = input[index].asset.ToString();
                output.definitions[index].explicitAssets = new[] { input[index] };
            }
            
            // Cache results
            if (useCache)
                SaveToCache(hash, output);
            return true;
        }

        private bool LoadFromCache(Hash128 hash, out BuildInput output)
        {
            return BuildCache.TryLoadCachedResults(hash, out output);
        }

        private void SaveToCache(Hash128 hash, BuildInput output)
        {
            BuildCache.SaveCachedResults(hash, output);
        }
    }
}
