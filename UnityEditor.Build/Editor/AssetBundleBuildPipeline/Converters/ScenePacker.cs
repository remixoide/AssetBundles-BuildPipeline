
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ScenePacker : IDataConverter<SceneLoadInfo, BuildCommandSet>
    {
        public uint Version { get { return 1; } }

        private const string kUnityDefaultResourcePath = "library/unity default resources";

        public bool Convert(SceneLoadInfo input, out BuildCommandSet output, bool useCache = true)
        {
            var sceneLoadInfo = new BuildCommandSet.AssetLoadInfo();
            sceneLoadInfo.asset = new GUID(AssetDatabase.AssetPathToGUID(input.scene));
            sceneLoadInfo.address = input.scene;
            sceneLoadInfo.referencedObjects = input.referencedObjects;

            var referencedObjects = new List<BuildCommandSet.SerializationInfo>();
            for (var i = 0; i < input.referencedObjects.Length; ++i)
            {
                if (input.referencedObjects[i].filePath == kUnityDefaultResourcePath)
                    continue;
                referencedObjects.Add(new BuildCommandSet.SerializationInfo { serializationIndex = Unity5Packer.CalculateSerializationIndexFromObjectIdentifier(input.referencedObjects[i]), serializationObject = input.referencedObjects[i] });
            }

            var scene = new BuildCommandSet.Command();
            scene.assetBundleName = Path.GetFileNameWithoutExtension(input.scene);
            scene.assetBundleObjects = referencedObjects.ToArray();
            //scene.assetBundleDependencies
            scene.explicitAssets = new[] { sceneLoadInfo };
            scene.scene = input.scene;
            scene.processedScene = input.processedScene;
            scene.globalUsage = input.globalUsage;

            output = new BuildCommandSet { commands = new [] { scene } };
            return true;
        }
    }
}