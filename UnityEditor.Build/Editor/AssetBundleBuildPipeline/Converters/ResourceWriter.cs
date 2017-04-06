using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ResourceWriter : IDataConverter<BuildCommandSet, BuildSettings, BuildOutput>
    {
        public long CalculateInputHash(BuildCommandSet commandSet, BuildSettings settings)
        {
            // TODO: Hash needs to include asset hashes
            return HashingMethods.CalculateMD5Hash(commandSet, settings.target, settings.group);
        }

        public bool Convert(BuildCommandSet commandSet, BuildSettings settings, out BuildOutput output)
        {
            // TODO: Validate commandSet & settings
            // TODO: Prepare settings.outputFolder
            Directory.CreateDirectory(settings.outputFolder);
            output = BuildInterface.WriteResourceFiles(commandSet, settings);
            return true;
        }
    }
}
