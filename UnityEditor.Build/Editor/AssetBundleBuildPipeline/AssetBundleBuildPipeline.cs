using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.Build
{
	public class AssetBundleBuildPipeline
	{
		[MenuItem("AssetBundles/Build Asset Bundles")]
		static void BuildAssetBundlesMenuItem()
		{
			var input = GenerateAssetBundleBuildInput();
			var settings = GenerateAssetBundleBuildSettings();

			var output = BuildAssetBundles(input, settings);
			foreach (var bundle in output.results)
			{
				var assetBundlePath = Path.Combine(settings.outputFolder, bundle.assetBundleName + ".bundle");
				AssetBundleBuildInterface.CompressAssetBundle(assetBundlePath, AssetBundleCompression.LZ4);
			}

			CacheAssetBundleBuildOutput(output, settings);
		}

		public static AssetBundleBuildInput GenerateAssetBundleBuildInput()
		{
			// TODO: Currently we do this at the low level as we cannot efficently walk the asset database from the high level yet
			return AssetBundleBuildInterface.GenerateAssetBundleBuildInput();
		}

		public static AssetBundleBuildSettings GenerateAssetBundleBuildSettings()
		{
			// TODO: settings at this point is unused but this is a rough idea of the struct usage
			var settings = new AssetBundleBuildSettings();
			settings.target = EditorUserBuildSettings.activeBuildTarget;
			settings.outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "AssetBundles/" + settings.target);
			settings.options = BuildAssetBundleOptions.None;
			return settings;
		}

		public static AssetBundleBuildCommandSet GenerateAssetBundleBuildCommandSet(AssetBundleBuildInput input, AssetBundleBuildSettings settings)
		{
			// Create commands array matching the size of the input
			var commands = new AssetBundleBuildCommandSet();
			commands.commands = new AssetBundleBuildCommandSet.Command[input.definitions.Length];
			for (var i = 0; i < input.definitions.Length; ++i)
			{
				var definition = input.definitions[i];

				// Populate each command from asset bundle definition
				var command = new AssetBundleBuildCommandSet.Command();
				command.assetBundleName = definition.assetBundleName;
				command.explicitAssets = new AssetBundleBuildCommandSet.AssetLoadInfo[definition.explicitAssets.Length];

				// Fill out asset load info and references for each asset in the definition
				var allObjects = new HashSet<ObjectIdentifier>();
				for (var j = 0; j < definition.explicitAssets.Length; ++j)
				{
					var explicitAsset = new AssetBundleBuildCommandSet.AssetLoadInfo();
					explicitAsset.asset = definition.explicitAssets[j];
					explicitAsset.includedObjects = AssetBundleBuildInterface.GetObjectIdentifiersInAsset(definition.explicitAssets[j]);
					explicitAsset.referencedObjects = AssetBundleBuildInterface.GetPlayerDependenciesForObjects(explicitAsset.includedObjects);

					command.explicitAssets[j] = explicitAsset;

					// TODO: This pulls in too much (IE: default assets, objects in other bundles, etc)
					allObjects.UnionWith(explicitAsset.includedObjects);
					allObjects.UnionWith(explicitAsset.referencedObjects);
				}

				// Fill out the array of all objects to be written out to this asset bundle
				// TODO: Filter based on hide flags, default assets, etc
				command.assetBundleObjects = allObjects.ToArray();

				commands.commands[i] = command;
			}

			return commands;
		}

		public static AssetBundleBuildOutput BuildAssetBundles(AssetBundleBuildInput input, AssetBundleBuildSettings settings)
		{
			var commands = GenerateAssetBundleBuildCommandSet(input, settings);
			return AssetBundleBuildInterface.ExecuteAssetBuildCommandSet(commands);
		}

		public static void CacheAssetBundleBuildOutput(AssetBundleBuildOutput output, AssetBundleBuildSettings settings)
		{
			// TODO: Cache data about this build result for future patching, incremental build, etc
		}
	}
}