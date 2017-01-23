#if EXAMPLE

using Unity.Bindings;
using UnityEngine;

namespace UnityEditor.Build
{
	public struct ObjectIdentifier
	{
		public GUID guid;
		public long localIdentifierInFile;
		public int type;

		public override string ToString()
		{
			return UnityString.Format("{{guid: {1}, fileID: {0}, type: {2}}}", guid, localIdentifierInFile, type);
		}
	}
	
	public struct AssetBundleBuildSettings
	{
		public string outputFolder;
		public BuildTarget target;
		public BuildAssetBundleOptions options;
	}
	
	public struct AssetBundleBuildInput
	{
		public struct Definition
		{
			public string assetBundleName;
			public GUID[] explicitAssets;
		}

		public Definition[] definitions;
	}
	
	public struct AssetBundleBuildCommandSet
	{
		public struct AssetLoadInfo
		{
			public GUID asset;
			public ObjectIdentifier[] includedObjects;
			public ObjectIdentifier[] referencedObjects;
		}
		
		public struct Command
		{
			public string assetBundleName;
			public AssetLoadInfo[] explicitAssets;
			public ObjectIdentifier[] assetBundleObjects;
			public string[] assetBundleDependencies;
		}

		public Command[] commands;
	}

	
	public struct AssetBundleBuildOutput
	{
		public struct ResourceFile
		{
			public string fileName;
			public bool serializedFile;
		}
		
		public struct Result
		{
			public string assetBundleName;
			public GUID[] explicitAssets;
			public ObjectIdentifier[] assetBundleObjects;
			public string[] assetBundleDependencies;
			public ResourceFile[] resourceFiles;
			public Hash128 targetHash;
			public Hash128 typeTreeLayoutHash;
			public System.Type[] includedTypes;
		}

		public Result[] results;
	}
	
	public enum AssetBundleCompression
	{
		Uncompressed,
		LZMA,
		LZ4
	}
	
	public class AssetBundleBuildInterface
	{
		// Generates an array of all asset bundles and the assets they include
		// Notes: We want to move this to C#, however we need extend the asset database api to do so. We felt it best to wait to do this until the new asset database lands.
		extern public static AssetBundleBuildInput GenerateAssetBundleBuildInput();

		// Get an array of all objects that are in an asset identified by GUID
		extern public static ObjectIdentifier[] GetObjectIdentifiersInAsset(GUID asset);

		// Get an array of all dependencies for an object identified by ObjectIdentifier
		// Notes: Due to the current asset database limitations, this api will only work for the currently active build target. We want to change this to take a built target, but will require new asset database.
		extern public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID);

		// Get an array of all dependencies for an array of objects identified by ObjectIdentifier.
		// Batch api to reduce C++ <> C# calls
		// Notes: Due to the current asset database limitations, this api will only work for the currently active build target. We want to change this to take a built target, but will require new asset database.
		extern public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs);

		// Writes out SerializedFile and Resource files for each bundle defined in AssetBundleBuildCommandSet
		extern public static AssetBundleBuildOutput ExecuteAssetBuildCommandSet(AssetBundleBuildCommandSet commands);

		// Archives and compresses SerializedFile and Resource files for a single asset bundle
		extern public static void CompressAssetBundle(AssetBundleBuildOutput.ResourceFile[] resourceFiles, AssetBundleCompression compression);

		// TODO: 
		// AssetBundleBuildSettings is still very much a work in progress. We are trying to figure out the best granularity of the struct vs each LLAPI call.
		// Incremental building of asset bundles
		// Maybe find some better names for types / fields
	}
}

#endif