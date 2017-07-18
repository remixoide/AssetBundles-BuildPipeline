using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class BuildInputDependency : IDataConverter<BuildInput, BuildSettings, string, BuildDependencyInformation>
    {
        private AssetDependency m_AssetDependency = new AssetDependency();
        private SceneDependency m_SceneDependency = new SceneDependency();

        public uint Version { get { return 1; } }

        public bool Convert(BuildInput input, BuildSettings settings, string outputFolder, out BuildDependencyInformation output, bool useCache = true)
        {
            output = new BuildDependencyInformation();
            foreach (var bundle in input.definitions)
            {
                foreach (var asset in bundle.explicitAssets)
                {
                    if (SceneDependency.ValidScene(asset.asset))
                    {
                        SceneLoadInfo sceneInfo;
                        if (!m_SceneDependency.Convert(asset.asset, settings, outputFolder, out sceneInfo, useCache))
                            continue;

                        var assetInfo = new BuildCommandSet.AssetLoadInfo();
                        assetInfo.asset = asset.asset;
                        assetInfo.address = asset.address;
                        assetInfo.processedScene = sceneInfo.processedScene;
                        assetInfo.includedObjects = new ObjectIdentifier[0];
                        assetInfo.referencedObjects = sceneInfo.referencedObjects;

                        output.sceneResourceFiles.Add(asset.asset, sceneInfo.resourceFiles);
                        output.sceneUsageTags.Add(asset.asset, sceneInfo.globalUsage);
                        output.assetLoadInfo.Add(asset.asset, assetInfo);
                        output.assetToBundle.Add(asset.asset, bundle.assetBundleName);
                    }
                    else if (AssetDependency.ValidAsset(asset.asset))
                    {
                        BuildCommandSet.AssetLoadInfo assetInfo;
                        if (!m_AssetDependency.Convert(asset.asset, settings, out assetInfo, useCache))
                            continue;

                        assetInfo.address = asset.address;
                        output.assetLoadInfo.Add(asset.asset, assetInfo);
                        output.assetToBundle.Add(asset.asset, bundle.assetBundleName);
                    }
                }
            }

            return true;
        }
    }
}
