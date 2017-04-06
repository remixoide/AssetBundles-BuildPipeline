using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ArchiveWriter : IDataConverter<BuildOutput, BuildCompression, string, uint[]>
    {
        public long CalculateInputHash(BuildOutput commandSet, BuildCompression compression, string outputFolder)
        {
            // TODO: may need to use the resource files as a hash input
            return HashingMethods.CalculateMD5Hash(commandSet, compression);
        }

        public bool Convert(BuildOutput output, BuildCompression compression, string outputFolder, out uint[] crc)
        {
            // TODO: Validate compression settings

            crc = new uint[output.results.Length];

            for (var i = 0; i < output.results.Length; i++)
            {
                Directory.CreateDirectory(outputFolder);
                var filePath = string.Format("{0}/{1}", outputFolder, output.results[i].assetBundleName);
                crc[i] = BuildInterface.ArchiveAndCompress(output.results[i].resourceFiles, filePath, compression);
            }
            return true;
        }
    }
}
