namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public interface IDataConverter
    {

    }

    public interface IDataConverter<in I, O> : IDataConverter
    {
        long CalculateInputHash(I input);
        bool Convert(I input, out O output);
    }

    public interface IDataConverter<in I1, in I2, O1> : IDataConverter
    {
        long CalculateInputHash(I1 input1, I2 input2);
        bool Convert(I1 input, I2 input2, out O1 output);
    }

    public interface IDataConverter<in I1, in I2, in I3, O1> : IDataConverter
    {
        long CalculateInputHash(I1 input1, I2 input2, I3 input3);
        bool Convert(I1 input, I2 input2, I3 input3, out O1 output);
    }
}