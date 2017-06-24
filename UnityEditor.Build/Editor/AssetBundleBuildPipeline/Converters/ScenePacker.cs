
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ScenePacker : IDataConverter<SceneLoadInfo[], BuildCommandSet>
    {
        public uint Version { get { return 1; } }

        private const string kUnityDefaultResourcePath = "library/unity default resources";

        public bool Convert(SceneLoadInfo[] input, out BuildCommandSet output, bool useCache = true)
        {
            GlobalUsageTags globalUsageTags = new GlobalUsageTags();

            var assetInfo = new List<BuildCommandSet.AssetLoadInfo>();
            var allObjects = new HashSet<ObjectIdentifier>();

            foreach (var sceneInput in input)
            {
                var sceneLoadInfo = new BuildCommandSet.AssetLoadInfo();
                sceneLoadInfo.asset = new GUID(AssetDatabase.AssetPathToGUID(sceneInput.scene));
                sceneLoadInfo.address = sceneInput.scene;
                sceneLoadInfo.processedScene = sceneInput.processedScene;
                sceneLoadInfo.referencedObjects = sceneInput.referencedObjects;

                assetInfo.Add(sceneLoadInfo);
                allObjects.UnionWith(sceneLoadInfo.referencedObjects);
            }

            var referencedObjects = new List<BuildCommandSet.SerializationInfo>();
            foreach (var refObj in allObjects)
            {

                if (refObj.filePath == kUnityDefaultResourcePath)
                    continue;

                referencedObjects.Add(new BuildCommandSet.SerializationInfo
                {
                    serializationIndex = Unity5Packer.CalculateSerializationIndexFromObjectIdentifier(refObj),
                    serializationObject = refObj
                });
            }

            var scene = new BuildCommandSet.Command();
            scene.assetBundleName = "TestScene";
            scene.assetBundleObjects = referencedObjects.ToArray();
            //scene.assetBundleDependencies
            scene.explicitAssets = assetInfo.ToArray();
            scene.globalUsage = globalUsageTags;
            scene.sceneBundle = true;

            output = new BuildCommandSet { commands = new [] { scene } };
            return true;
        }
    }
}