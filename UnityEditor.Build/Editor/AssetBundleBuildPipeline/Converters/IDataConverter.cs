namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public interface IDataConverter
    {

    }

    public interface IDataConverter<in I, O>
    {
        int GetInputHash(I input);
        bool Convert(I input, out O output);
    }

    public interface IDataConverter<in I1, in I2, O1>
    {
        int GetInputHash(I1 input1, I2 input2);
        bool Convert(I1 input, I2 input2, out O1 output);
    }
}