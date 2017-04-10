using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Utilities
{
    public static class CompareFuncs
    {
        public static int Compare(ObjectIdentifier x, ObjectIdentifier y)
        {
            if (x.guid != y.guid)
                return x.guid.CompareTo(y.guid);

            // Notes: Only if both guids are invalid, we should check path first
            var empty = new GUID();
            if (x.guid == empty && y.guid == empty)
                return x.filePath.CompareTo(y.filePath);

            if (x.localIdentifierInFile != y.localIdentifierInFile)
                return x.localIdentifierInFile.CompareTo(y.localIdentifierInFile);

            return x.fileType.CompareTo(y.fileType);
        }

        public static int Compare(BuildCommandSet.SerializationInfo x, BuildCommandSet.SerializationInfo y)
        {
            if (x.serializationIndex != y.serializationIndex)
                return x.serializationIndex.CompareTo(y.serializationIndex);

            return Compare(x.serializationObject, y.serializationObject);
        }
    }

    public class SerializationInfoComparer : IComparer<BuildCommandSet.SerializationInfo>
    {
        public int Compare(BuildCommandSet.SerializationInfo x, BuildCommandSet.SerializationInfo y)
        {
            return CompareFuncs.Compare(x, y);
        }
    }

    public class ObjectIdentifierComparer : IComparer<ObjectIdentifier>
    {
        public int Compare(ObjectIdentifier x, ObjectIdentifier y)
        {
            return CompareFuncs.Compare(x, y);
        }
    }
}