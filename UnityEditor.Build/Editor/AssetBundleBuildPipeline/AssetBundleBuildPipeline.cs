using System;
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
			var input = AssetBundleBuildInterface.GenerateAssetBundleBuildInput();
			var settings = GenerateAssetBundleBuildSettings();
			var compression = GenerateBuildCompression();
			var commands = GenerateAssetBundleBuildCommandSet(input, settings);

			// Ensure the output path is created
			// TODO: mabe we should do something if it exists, incremental building?
			Directory.CreateDirectory(settings.outputFolder);
			var output = AssetBundleBuildInterface.WriteResourcefilesForAssetBundles(commands, settings);
			foreach (var bundle in output.results)
				AssetBundleBuildInterface.ArchiveAndCompressAssetBundle(bundle.resourceFiles, Path.Combine(settings.outputFolder, bundle.assetBundleName), compression);

			CacheAssetBundleBuildOutput(output, settings);
		}

		public static AssetBundleBuildSettings GenerateAssetBundleBuildSettings()
		{
			var settings = new AssetBundleBuildSettings();
			settings.target = EditorUserBuildSettings.activeBuildTarget;
			settings.outputFolder = "AssetBundles/" + settings.target;
			settings.streamingResources = true;
			settings.editorBundles = false;
			return settings;
		}

		public static BuildCompression GenerateBuildCompression()
		{
			BuildCompression compression;
			compression.compression = CompressionType.Lz4HC;
			compression.streamed = false;
			compression.level = CompressionLevel.Maximum;
			compression.blockSize = AssetBundleBuildInterface.DefaultCompressionBlockSize;
			return compression;
		}

		private static void DebugPrintCommandSet(ref AssetBundleBuildCommandSet commandSet)
		{
			var msg = "";
			if (commandSet.commands != null)
				foreach (var bundle in commandSet.commands)
				{
					msg += string.Format("{0}\n", bundle.assetBundleName);
					if (bundle.explicitAssets != null)
						foreach (var asset in bundle.explicitAssets)
						{
							msg += string.Format("\t{0}\n", asset.asset);
							if (asset.includedObjects != null)
								foreach (var obj in asset.includedObjects)
									msg += string.Format("\t\t{0}\n", obj);
							msg += "\t\t------------------------------\n";
							if (asset.referencedObjects != null)
								foreach (var obj in asset.referencedObjects)
									msg += string.Format("\t\t{0}\n", obj);
						}
					if (bundle.assetBundleObjects != null)
						foreach (var obj in bundle.assetBundleObjects)
							msg += string.Format("\t{0}\n", obj);
					if (bundle.assetBundleDependencies != null)
						foreach (var dependency in bundle.assetBundleDependencies)
							msg += string.Format("\t{0}\n", dependency);
				}

			UnityEngine.Debug.Log(msg);
		}

		public static AssetBundleBuildCommandSet GenerateAssetBundleBuildCommandSet(AssetBundleBuildInput input, AssetBundleBuildSettings settings)
		{
			// Create commands array matching the size of the input
			var commandSet = new AssetBundleBuildCommandSet();
			commandSet.commands = new AssetBundleBuildCommandSet.Command[input.definitions.Length];
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
					allObjects.UnionWith(explicitAsset.includedObjects);
					allObjects.UnionWith(explicitAsset.referencedObjects);
				}

				command.assetBundleObjects = allObjects.ToArray();
				commandSet.commands[i] = command;
			}
			
			// TODO: Debug printing
			//DebugPrintCommandSet(ref commandSet);

			// At this point, We have generated fully self contained asset bundles with 0 dependencies.
			// Default implementation is to reduce duplication of objects by declaring dependencies to other asset
			//    bundles if that other asset bundle has an explicit asset declared that contains the objects needed
			// We also remove any built in unity objects as they are built with the player (We may want to change this part in the future)
			CalculateAssetBundleBuildDependencies(ref commandSet);
			// Note: I may, or may not feel dirty doing mutable things to what otherwise should be immutable struct

			// TODO: Debug printing
			//DebugPrintCommandSet(ref commandSet);

			return commandSet;
		}

		public static void CalculateAssetBundleBuildDependencies(ref AssetBundleBuildCommandSet commandSet)
		{
			// Dictionary for quick included asset lookup
			var assetToBundleMap = new Dictionary<GUID, string>();
			for (var i = 0; i < commandSet.commands.Length; ++i)
			{
				var bundle = commandSet.commands[i];
				foreach (var asset in bundle.explicitAssets)
					assetToBundleMap.Add(asset.asset, commandSet.commands[i].assetBundleName);
			}

			for (var i = 0; i < commandSet.commands.Length; ++i)
			{
				CalculateAssetBundleDependencies(ref commandSet.commands[i], assetToBundleMap);
			}
		}

		private static void CalculateAssetBundleDependencies(ref AssetBundleBuildCommandSet.Command bundle, Dictionary<GUID, string> assetToBundleMap)
		{
			var allObjects = new List<ObjectIdentifier>(bundle.assetBundleObjects);
			var dependencies = new HashSet<string>();
			for (var i = allObjects.Count - 1; i >= 0; --i)
			{
				// Remove built in unity objects (We may want to change this part in the future)
				if (allObjects[i].type == 0)
				{
					allObjects.RemoveAt(i);
					continue;
				}

				// Check to see if the asset of this object is already explicityly in a bundle
				var dependency = "";
				if (!assetToBundleMap.TryGetValue(allObjects[i].guid, out dependency) || dependency == bundle.assetBundleName)
					continue;

				dependencies.Add(dependency);
				allObjects.RemoveAt(i);
			}
			bundle.assetBundleObjects = allObjects.ToArray();
			bundle.assetBundleDependencies = dependencies.ToArray();
		}

		public static void CacheAssetBundleBuildOutput(AssetBundleBuildOutput output, AssetBundleBuildSettings settings)
		{
			// TODO: Cache data about this build result for future patching, incremental build, etc
		}
	}
}