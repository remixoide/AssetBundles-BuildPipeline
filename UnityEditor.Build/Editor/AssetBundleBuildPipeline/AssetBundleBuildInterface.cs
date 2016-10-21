using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace UnityEditor.Build
{
	public struct ObjectIdentifier
	{
		public GUID guid;
		public UInt64 localIdentifierInFile;
	}

	public class AssetBundleBuildSettings
	{
		public string outputFolder;
		public BuildTarget target;
		public BuildAssetBundleOptions options;
	}

	public struct AssetBundleBuildInput
	{
		public struct Definition
		{
			public string name;
			public string variant;
			public GUID[] assets;
		}

		public AssetBundleBuildSettings settings;
		public Definition[] bundles;
	}

	public struct AssetBundleBuildCommandSet
	{
		public struct Command
		{
			public AssetBundleBuildInput.Definition input;
			public ObjectIdentifier[] objectsToBeWritten;
		}

		public AssetBundleBuildSettings settings;
		public Command[] commands;
	}

	public struct AssetBundleBuildOutput
	{
		public struct Result
		{
			public string assetBundleName;
			public Hash128 targetHash;
			public Hash128 typeTreeLayoutHash;
			public GUID[] explicitlyIncludedAssets;
			public ObjectIdentifier[] writtenObjects;
			public string[] assetBundleDependencies;
			public Type[] includedTypes;
		}
		public Result[] results;
	}

	public class AssetBundleBuildInterface
	{
		[MenuItem("AssetBundles/Build Asset Bundles")]
		static void BuildAssetBundlesMenuItem()
		{
			var settings = new AssetBundleBuildSettings();
			SaveAssetBundleOutput(ExecuteAssetBuildCommandSet(GenerateAssetBuildInstructionSet(GenerateAssetBundleBuildInput(settings))));
		}

		public static AssetBundleBuildInput GenerateAssetBundleBuildInput(AssetBundleBuildSettings settings)
		{
			var input = new AssetBundleBuildInput();
			input.settings = settings;
			var bundleNames = AssetDatabase.GetAllAssetBundleNames();
			input.bundles = new AssetBundleBuildInput.Definition[bundleNames.Length];
			for (int i = 0; i < bundleNames.Length; i++)
			{
				int dot = bundleNames[i].LastIndexOf('.');
				input.bundles[i].name = dot < 0 ? bundleNames[i] : bundleNames[i].Substring(0, dot);
				input.bundles[i].variant = dot < 0 ? string.Empty : bundleNames[i].Substring(dot + 1);
				var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleNames[i]);
				input.bundles[i].assets = new GUID[assets.Length];
				for (int a = 0; a < assets.Length; a++)
					input.bundles[i].assets[a] = new GUID(AssetDatabase.AssetPathToGUID(assets[a]));
			}
			
			return input;
		}

		public static AssetBundleBuildCommandSet GenerateAssetBuildInstructionSet(AssetBundleBuildInput buildInput)
		{
			AssetBundleBuildCommandSet cmdSet = new AssetBundleBuildCommandSet();
			cmdSet.commands = new AssetBundleBuildCommandSet.Command[buildInput.bundles.Length];
			List<HashSet<ObjectIdentifier>> objectReferences = new List<HashSet<ObjectIdentifier>>();
			for(int i = 0; i <buildInput.bundles.Length; i++)
			{
				cmdSet.commands[i].input = buildInput.bundles[i];
				HashSet<ObjectIdentifier> objects = new HashSet<ObjectIdentifier>();
				foreach (var asset in buildInput.bundles[i].assets)
				{
					foreach (var o in GetObjectIdentifiersInAsset(asset))
					{
						objects.Add(o);
						foreach (var d in GetPlayerDependenciesForObject(o))
							objects.Add(d);
					}
				}
				objectReferences.Add(objects);
			}

			//stripping - this is REALLY bad... O^4 complexity...
			List<ObjectIdentifier> toRemove = new List<ObjectIdentifier>();
			for(int i = 0; i < objectReferences.Count; i++)
			{
				var refs = objectReferences[i];
				//strip here
				foreach (var o in refs)
				{
					for( int bi = 0; bi > cmdSet.commands.Length; bi++)
					{
						if (bi == i)
							continue;
						var bc = cmdSet.commands[bi];
						if (bc.input.assets.Contains(o.guid))
						{
							toRemove.Add(o);
						}
					}
				}
				foreach (var r in toRemove)
					refs.Remove(r);
				cmdSet.commands[i].objectsToBeWritten = refs.ToArray();
			}
			return cmdSet;
		}

		public static void SaveAssetBundleOutput(AssetBundleBuildOutput output)
		{
			//TODO: C#
		}

		public static ObjectIdentifier[] GetObjectIdentifiersInAsset(GUID asset)
		{
			//TODO: C++
			return null;
		}

		public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier obj)
		{
			//TODO: C++
			return null;
		}

		public static AssetBundleBuildOutput ExecuteAssetBuildCommandSet(AssetBundleBuildCommandSet set)
		{
			//TODO: C++
			return new AssetBundleBuildOutput();
		}

	}
}