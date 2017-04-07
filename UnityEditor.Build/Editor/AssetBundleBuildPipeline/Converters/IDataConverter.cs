﻿using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public interface IDataConverter
    {

    }

    public interface IDataConverter<in I, O> : IDataConverter
    {
        Hash128 CalculateInputHash(I input);
        bool Convert(I input, out O output);
        bool LoadFromCacheOrConvert(I input, out O output);
    }

    public interface IDataConverter<in I1, in I2, O1> : IDataConverter
    {
        Hash128 CalculateInputHash(I1 input1, I2 input2);
        bool Convert(I1 input, I2 input2, out O1 output);
        bool LoadFromCacheOrConvert(I1 input, I2 input2, out O1 output);
    }

    public interface IDataConverter<in I1, in I2, in I3, O1> : IDataConverter
    {
        Hash128 CalculateInputHash(I1 input1, I2 input2, I3 input3);
        bool Convert(I1 input, I2 input2, I3 input3, out O1 output);
        bool LoadFromCacheOrConvert(I1 input, I2 input2, I3 input3, out O1 output);
    }

    public interface IDataConverter<in I1, in I2, in I3, in I4, O1> : IDataConverter
    {
        Hash128 CalculateInputHash(I1 input1, I2 input2, I3 input3, I4 input4);
        bool Convert(I1 input, I2 input2, I3 input3, I4 input4, out O1 output);
        bool LoadFromCacheOrConvert(I1 input, I2 input2, I3 input3, I4 input4, out O1 output);
    }
}