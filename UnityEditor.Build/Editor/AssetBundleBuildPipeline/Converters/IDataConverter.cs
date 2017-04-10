using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public interface IDataConverter
    {
        uint Version { get; }
    }

    public interface IDataConverter<in I, O> : IDataConverter
    {
        bool Convert(I input, out O output, bool useCache = true);
    }

    public interface IDataConverter<in I1, in I2, O1> : IDataConverter
    {
        bool Convert(I1 input, I2 input2, out O1 output, bool useCache = true);
    }

    public interface IDataConverter<in I1, in I2, in I3, O1> : IDataConverter
    {
        bool Convert(I1 input, I2 input2, I3 input3, out O1 output, bool useCache = true);
    }

    public interface IDataConverter<in I1, in I2, in I3, in I4, O1> : IDataConverter
    {
        bool Convert(I1 input, I2 input2, I3 input3, I4 input4, out O1 output, bool useCache = true);
    }
}