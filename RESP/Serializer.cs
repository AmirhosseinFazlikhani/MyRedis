using System.Text;

namespace RESP;

public static class Serializer
{
    private static readonly byte[] Terminator = "\r\n"u8.ToArray();

    public static byte[] SerializeSimpleString(string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return CreateSimpleSentence(DataTypes.SimpleString, valueBytes);
    }

    public static byte[] SerializeSimpleError(string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        return CreateSimpleSentence(DataTypes.SimpleError, valueBytes);
    }

    public static byte[] SerializeInteger(int value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        return CreateSimpleSentence(DataTypes.Integer, valueBytes);
    }

    public static byte[] SerializeBulkString(string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var valueLengthBytes = BitConverter.GetBytes((uint)valueBytes.Length);

        // $<length>\r\n<data>\r\n
        // 1,   4   , 2 ,     , 2    = 9
        var result = new byte[9 + valueBytes.Length];
        result[0] = DataTypes.BulkString;
        Buffer.BlockCopy(valueLengthBytes, 0, result, 1, valueLengthBytes.Length);
        Buffer.BlockCopy(Terminator, 0, result, 5, Terminator.Length);
        Buffer.BlockCopy(valueBytes, 0, result, 7, value.Length);
        Buffer.BlockCopy(Terminator, 0, result, result.Length - Terminator.Length, Terminator.Length);

        return result;
    }

    public static byte[] SerializeNull()
    {
        return [DataTypes.Null, Terminator[0], Terminator[1]];
    }

    public static byte[] SerializeBoolean(bool value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        return CreateSimpleSentence(DataTypes.Boolean, valueBytes);
    }

    public static byte[] SerializeDouble(double value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        return CreateSimpleSentence(DataTypes.Double, valueBytes);
    }

    public static byte[] SerializeBigNumber(long value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        return CreateSimpleSentence(DataTypes.BigNumber, valueBytes);
    }

    public static byte[] SerializeBulkError(string value)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var valueLengthBytes = BitConverter.GetBytes((uint)valueBytes.Length);

        // !<length>\r\n<data>\r\n
        // 1,   4   , 2 ,     , 2    = 9
        var result = new byte[9 + valueBytes.Length];
        result[0] = DataTypes.BulkError;
        Buffer.BlockCopy(valueLengthBytes, 0, result, 1, valueLengthBytes.Length);
        Buffer.BlockCopy(Terminator, 0, result, 5, Terminator.Length);
        Buffer.BlockCopy(valueBytes, 0, result, 7, value.Length);
        Buffer.BlockCopy(Terminator, 0, result, result.Length - Terminator.Length, Terminator.Length);

        return result;
    }

    public static byte[] SerializeVerbatimString(string value, string encoding)
    {
        const int encodingLength = 3;

        if (encoding.Length != 3)
        {
            throw new ArgumentException("Argument length should be 3", nameof(encoding));
        }

        var valueBytes = Encoding.UTF8.GetBytes(value);
        var valueLengthBytes = BitConverter.GetBytes((uint)valueBytes.Length);
        var encodingBytes = Encoding.UTF8.GetBytes(encoding);

        // =<length>\r\n<encoding>:<data>\r\n
        // 1,   4   , 2 ,    3   ,1,     , 2    = 13
        var result = new byte[13 + valueBytes.Length];
        result[0] = DataTypes.VerbatimString;
        Buffer.BlockCopy(valueLengthBytes, 0, result, 1, valueLengthBytes.Length);
        Buffer.BlockCopy(Terminator, 0, result, 5, Terminator.Length);
        Buffer.BlockCopy(encodingBytes, 0, result, 7, encodingLength);
        result[10] = (byte)':';
        Buffer.BlockCopy(valueBytes, 0, result, 11, value.Length);
        Buffer.BlockCopy(Terminator, 0, result, result.Length - Terminator.Length, Terminator.Length);

        return result;
    }

    public static byte[] SerializeArray(byte[][] values)
    {
        var arrayValue = values.Aggregate((p, c) =>
            {
                var result = new byte[p.Length + c.Length];
                Buffer.BlockCopy(p, 0, result, 0, p.Length);
                Buffer.BlockCopy(c, 0, result, p.Length, c.Length);
                return result;
            })
            .ToArray();

        var valuesLengthBytes = BitConverter.GetBytes(values.Length);

        var result = new byte[6 + values.Sum(v => v.Length)];
        result[0] = DataTypes.Array;
        Buffer.BlockCopy(valuesLengthBytes, 0, result, 1, valuesLengthBytes.Length);
        Buffer.BlockCopy(arrayValue, 0, result, 5, arrayValue.Length);
        Buffer.BlockCopy(Terminator, 0, result, result.Length - Terminator.Length, Terminator.Length);

        return result;
    }

    private static byte[] CreateSimpleSentence(byte dataType, byte[] value)
    {
        var result = new byte[1 + value.Length + Terminator.Length];
        result[0] = dataType;
        Buffer.BlockCopy(value, 0, result, 1, value.Length);
        Buffer.BlockCopy(Terminator, 0, result, result.Length - Terminator.Length, Terminator.Length);

        return result;
    }
}