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

	public enum CompressionType
	{
		None,
		Lzma,
		Lz4,
		Lz4HC,
		Lzham,
	}

	public enum CompressionLevel
	{
		None,
		Fastest,
		Fast,
		Normal,
		High,
		Maximum,
	}
	
	public struct BuildCompression
	{
		public CompressionType compression;
		public CompressionLevel level;
		public uint blockSize;
		public bool streamed;
	}

	public struct AssetBundleBuildSettings
	{
		public string outputFolder;
		public BuildTarget target;
		public bool streamingResources;
		public bool editorBundles;
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
	
	public class AssetBundleBuildInterface
	{
		// Default block size compression to be used with BuildCompression struct
		public const uint DefaultCompressionBlockSize = 131072; //128 * 1024;

		// Generates an array of all asset bundles and the assets they include
		// Notes: Pre-dreprecated as we want to move asset bundle data off of asset meta files and into it's own asset
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
		extern public static AssetBundleBuildOutput WriteResourcefilesForAssetBundles(AssetBundleBuildCommandSet commands, AssetBundleBuildSettings settings));

		// Archives and compresses SerializedFile and Resource files for a single asset bundle
		extern public static void ArchiveAndCompressAssetBundle(AssetBundleBuildOutput.ResourceFile[] resourceFiles, string outputBundlePath, BuildCompression compression);

		// TODO: 
		// Incremental building of asset bundles
		// Maybe find some better names for some types / fields. IE: AssetBundleBuildCommandSet.Command is kinda awkward
	}
}

#endif