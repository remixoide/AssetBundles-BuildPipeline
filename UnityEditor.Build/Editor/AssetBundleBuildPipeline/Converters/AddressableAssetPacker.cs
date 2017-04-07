using UnityEditor.Build.Cache;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class AddressableAssetPacker : IDataConverter<BuildInput.AddressableAsset[], BuildInput>
    {
        public Hash128 CalculateInputHash(BuildInput.AddressableAsset[] input)
        {
            return HashingMethods.CalculateMD5Hash(input);
        }

        public bool Convert(BuildInput.AddressableAsset[] input, out BuildInput output)
        {
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
            return true;
        }

        public bool LoadFromCacheOrConvert(BuildInput.AddressableAsset[] input, out BuildInput output)
        {
            var hash = CalculateInputHash(input);
            if (BuildCache.TryLoadCachedResults(hash, out output))
                return true;

            if (!Convert(input, out output))
                return false;

            BuildCache.SaveCachedResults(hash, output);
            return true;
        }
    }
}
