using System.Text;

namespace RESP;

public static class Serializer
{
    private const string Terminator = "\r\n";
    private static readonly byte[] TerminatorBytes = "\r\n"u8.ToArray();
    private const string PositiveInfinity = "inf";
    private const string NegativeInfinity = "-inf";

    public static byte[] SerializeSimpleString(string value)
    {
        ValidateSimpleText(value);
        return Encoding.UTF8.GetBytes($"{DataTypes.SimpleString}{value}{Terminator}");
    }

    public static byte[] SerializeSimpleError(string value)
    {
        ValidateSimpleText(value);
        return Encoding.UTF8.GetBytes($"{DataTypes.SimpleError}{value}{Terminator}");
    }

    public static byte[] SerializeInteger(long value)
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.Integer}{value}{Terminator}");
    }

    public static byte[] SerializeBulkString(string value)
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.BulkString}{value.Length}{Terminator}{value}{Terminator}");
    }

    public static byte[] SerializeNull()
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.Null}{Terminator}");
    }

    public static byte[] SerializeBoolean(bool value)
    {
        var valueChar = value ? 't' : 'f';
        return Encoding.UTF8.GetBytes($"{DataTypes.Boolean}{valueChar}{Terminator}");
    }

    public static byte[] SerializeDouble(double value)
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.Double}{value}{Terminator}");
    }

    public static byte[] SerializeBigNumber(string value)
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.BigNumber}{value}{Terminator}");
    }

    public static byte[] GetPositiveInfinity()
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.Double}{PositiveInfinity}{Terminator}");
    }

    public static byte[] GetNegativeInfinity()
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.Double}{NegativeInfinity}{Terminator}");
    }

    public static byte[] SerializeBulkError(string value)
    {
        return Encoding.UTF8.GetBytes($"{DataTypes.BulkError}{value.Length}{Terminator}{value}{Terminator}");
    }

    public static byte[] SerializeVerbatimString(string value, string encoding)
    {
        if (encoding.Length != 3)
        {
            throw new ArgumentException("Argument length should be 3", nameof(encoding));
        }

        return Encoding.UTF8.GetBytes(
            $"{DataTypes.VerbatimString}{value.Length}{Terminator}{encoding}:{value}{Terminator}");
    }

    public static byte[] SerializeArray(byte[][] values)
    {
        if (values.Length == 0)
        {
            return Encoding.UTF8.GetBytes($"{DataTypes.Array}0{Terminator}");
        }
        
        var arrayLengthBytes =  Encoding.UTF8.GetBytes(values.Length.ToString());
        var result = new byte[3 + arrayLengthBytes.Length + values.Sum(v => v.Length)];
        result[0] = (byte)DataTypes.Array;
        var resultOffset = 1;
        Buffer.BlockCopy(arrayLengthBytes, 0, result, resultOffset, arrayLengthBytes.Length);
        resultOffset += arrayLengthBytes.Length;
        Buffer.BlockCopy(TerminatorBytes, 0, result, resultOffset, TerminatorBytes.Length);
        resultOffset += TerminatorBytes.Length;
        
        foreach (var value in values)
        {
            Buffer.BlockCopy(value, 0, result, resultOffset, value.Length);
            resultOffset += value.Length;
        }

        return result;
    }

    private static void ValidateSimpleText(string value)
    {
        if (value.Any(c => c is '\n' or '\r'))
        {
            throw new ArgumentException("Invalid character", nameof(value));
        }
    }
}