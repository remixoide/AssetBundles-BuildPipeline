using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Utilities
{
    public class SerializationInfoComparer : IComparer<BuildCommandSet.SerializationInfo>
    {
        public int Compare(BuildCommandSet.SerializationInfo x, BuildCommandSet.SerializationInfo y)
        {
            if (x.serializationIndex != y.serializationIndex)
                return x.serializationIndex.CompareTo(y.serializationIndex);

            if (x.serializationObject.guid != y.serializationObject.guid)
                return x.serializationObject.guid.CompareTo(y.serializationObject.guid);
            
            // Notes: Only if both guids are invalid, we should check path first
            var empty = new GUID();
            if (x.serializationObject.guid == empty && y.serializationObject.guid == empty)
                return x.serializationObject.filePath.CompareTo(y.serializationObject.filePath);

            if (x.serializationObject.localIdentifierInFile != y.serializationObject.localIdentifierInFile)
                return x.serializationObject.localIdentifierInFile.CompareTo(y.serializationObject.localIdentifierInFile);

            return x.serializationObject.fileType.CompareTo(y.serializationObject.fileType);
        }
    }
}