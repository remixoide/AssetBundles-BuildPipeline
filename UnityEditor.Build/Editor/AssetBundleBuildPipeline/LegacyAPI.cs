using UnityEditor.Build.AssetBundle;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build
{
    public static partial class BuildPipeline
    {
        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var playerSettings = AssetBundleBuildPipeline.GenerateBuildPlayerSettings();
            playerSettings.target = targetPlatform;
            var playerResults = PlayerBuildInterface.CompilePlayerScripts(playerSettings);
            
            var bundleSettings = AssetBundleBuildPipeline.GenerateBuildSettings();
            bundleSettings.target = targetPlatform;
            bundleSettings.typeDB = playerResults.typeDB;

            BuildCompression compression = BuildCompression.DefaultLZMA;
            if ((assetBundleOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                compression = BuildCompression.DefaultLZ4;
            else if ((assetBundleOptions & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                compression = BuildCompression.DefaultUncompressed;

            AssetBundleBuildPipeline.BuildAssetBundles(BuildInterface.GenerateBuildInput(), bundleSettings, compression);
            return null;
            //return UnityEditor.BuildPipeline.BuildAssetBundles(outputPath, assetBundleOptions, targetPlatform);
        }

        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var playerSettings = AssetBundleBuildPipeline.GenerateBuildPlayerSettings();
            playerSettings.target = targetPlatform;
            var playerResults = PlayerBuildInterface.CompilePlayerScripts(playerSettings);

            var bundleSettings = AssetBundleBuildPipeline.GenerateBuildSettings();
            bundleSettings.target = targetPlatform;
            bundleSettings.typeDB = playerResults.typeDB;

            BuildCompression compression = BuildCompression.DefaultLZMA;
            if ((assetBundleOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                compression = BuildCompression.DefaultLZ4;
            else if ((assetBundleOptions & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                compression = BuildCompression.DefaultUncompressed;

            BuildInput buildInput;
            var converter = new AssetBundleBuildConverter();
            if (!converter.Convert(builds, out buildInput))
                return null;

            AssetBundleBuildPipeline.BuildAssetBundles(buildInput, bundleSettings, compression);
            return null;
            //return UnityEditor.BuildPipeline.BuildAssetBundles(outputPath, builds, assetBundleOptions, targetPlatform);
        }
    }
}