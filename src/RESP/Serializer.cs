using System.Text;

namespace RESP;

public static class Serializer
{
    public const string Terminator = "\r\n";
    public static readonly byte[] TerminatorBytes = [.."\r\n"u8];
    private const string PositiveInfinity = "inf";
    private const string NegativeInfinity = "-inf";

    public static string SerializeSimpleString(string value)
    {
        ValidateSimpleText(value);
        return $"{DataTypePrefixes.SimpleString}{value}{Terminator}";
    }

    public static string SerializeSimpleError(string value)
    {
        ValidateSimpleText(value);
        return $"{DataTypePrefixes.SimpleError}{value}{Terminator}";
    }

    public static string SerializeInteger(long value)
    {
        return $"{DataTypePrefixes.Integer}{value}{Terminator}";
    }

    public static string SerializeBulkString(string value)
    {
        return $"{DataTypePrefixes.BulkString}{value.Length}{Terminator}{value}{Terminator}";
    }

    public static string SerializeNull()
    {
        return $"{DataTypePrefixes.Null}{Terminator}";
    }

    public static string SerializeBoolean(bool value)
    {
        var valueChar = value ? 't' : 'f';
        return $"{DataTypePrefixes.Boolean}{valueChar}{Terminator}";
    }

    public static string SerializeDouble(double value)
    {
        return $"{DataTypePrefixes.Double}{value}{Terminator}";
    }

    public static string SerializeBigNumber(string value)
    {
        return $"{DataTypePrefixes.BigNumber}{value}{Terminator}";
    }

    public static string GetPositiveInfinity()
    {
        return $"{DataTypePrefixes.Double}{PositiveInfinity}{Terminator}";
    }

    public static string GetNegativeInfinity()
    {
        return $"{DataTypePrefixes.Double}{NegativeInfinity}{Terminator}";
    }

    public static string SerializeBulkError(string value)
    {
        return $"{DataTypePrefixes.BulkError}{value.Length}{Terminator}{value}{Terminator}";
    }

    public static string SerializeVerbatimString(string value, string encoding)
    {
        if (encoding.Length != 3)
        {
            throw new ArgumentException("Argument length should be 3", nameof(encoding));
        }

        return
            $"{DataTypePrefixes.VerbatimString}{value.Length}{Terminator}{encoding}:{value}{Terminator}";
    }

    public static string SerializeArray(string[] values)
    {
        if (values.Length == 0)
        {
            return $"{DataTypePrefixes.Array}0{Terminator}";
        }

        var serializedValue = new StringBuilder(values.Sum(v => v.Length));

        foreach (var value in values)
        {
            serializedValue.Append(value);
        }

        return $"{DataTypePrefixes.Array}{values.Length}{Terminator}{serializedValue}";
    }

    public static string SerializerMap(KeyValuePair<string, string>[] entries)
    {
        var builder = new StringBuilder($"{DataTypePrefixes.Map}{entries.Length}{Terminator}");

        foreach (var entry in entries)
        {
            builder.Append(SerializeSimpleString(entry.Key));
            builder.Append(entry.Value);
        }

        return builder.ToString();
    }

    private static void ValidateSimpleText(string value)
    {
        if (value.Any(c => c is '\n' or '\r'))
        {
            throw new ArgumentException("Invalid character", nameof(value));
        }
    }
}