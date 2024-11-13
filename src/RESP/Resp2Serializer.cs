using System.Globalization;
using System.Text;
using RESP.DataTypes;

namespace RESP;

public static class Resp2Serializer
{
    public const string Terminator = "\r\n";
    public static readonly byte[] TerminatorBytes = [.."\r\n"u8];

    public static string Serialize(RespSimpleString data)
    {
        ValidateSimpleText(data.Value);
        return $"{RespSimpleString.Prefix}{data.Value}{Terminator}";
    }

    public static string Serialize(RespSimpleError data)
    {
        ValidateSimpleText(data.Value);
        return $"{RespSimpleError.Prefix}{data.Value}{Terminator}";
    }

    public static string Serialize(RespInteger data)
    {
        return $"{RespInteger.Prefix}{data.Value}{Terminator}";
    }

    public static string Serialize(RespBulkString data)
    {
        return data.Value is null
            ? $"{RespBulkString.Prefix}-1{Terminator}"
            : $"{RespBulkString.Prefix}{data.Value.Length}{Terminator}{data.Value}{Terminator}";
    }

    public static string Serialize(RespBoolean data)
    {
        var valueChar = data.Value ? 't' : 'f';
        return $"{RespBoolean.Prefix}{valueChar}{Terminator}";
    }

    public static string Serialize(RespDouble data)
    {
        return $"{RespDouble.Prefix}{data.Value}{Terminator}";
    }

    public static string Serialize(RespBigNumber data)
    {
        return $"{RespBigNumber.Prefix}{data.Value}{Terminator}";
    }

    public static string Serialize(RespBulkError data)
    {
        return $"{RespBulkError.Prefix}{data.Value.Length}{Terminator}{data.Value}{Terminator}";
    }

    public static string Serialize(RespVerbatimString data)
    {
        if (data.Encoding.Length != 3)
        {
            throw new ArgumentException("Encoding length should be 3", nameof(data.Encoding));
        }

        return
            $"{RespVerbatimString.Prefix}{data.Value.Length}{Terminator}{data.Encoding}:{data.Value}{Terminator}";
    }

    public static string Serialize(RespArray data)
    {
        if (data.Items.Length == 0)
        {
            return $"{RespArray.Prefix}0{Terminator}";
        }

        var serializedValue = new StringBuilder();

        foreach (var item in data.Items)
        {
            var serializedItem = (string)Serialize((dynamic)item);
            serializedValue.Append(serializedItem);
        }

        return $"{RespArray.Prefix}{data.Items.Length}{Terminator}{serializedValue}";
    }

    public static List<IRespData> Deserialize(byte[] buffer)
    {
        var result = new List<IRespData>();
        var position = 0;

        while (position < buffer.Length)
        {
            result.Add(ParseRespData(buffer, ref position));
        }

        return result;
    }

    private static IRespData ParseRespData(byte[] buffer, ref int position)
    {
        var prefix = (char)buffer[position++];

        return prefix switch
        {
            RespSimpleString.Prefix => ParseSimpleString(buffer, ref position),
            RespSimpleError.Prefix => ParseSimpleError(buffer, ref position),
            RespInteger.Prefix => ParseInteger(buffer, ref position),
            RespBulkString.Prefix => ParseBulkString(buffer, ref position),
            RespBoolean.Prefix => ParseBoolean(buffer, ref position),
            RespDouble.Prefix => ParseDouble(buffer, ref position),
            RespBigNumber.Prefix => ParseBigNumber(buffer, ref position),
            RespBulkError.Prefix => ParseBulkError(buffer, ref position),
            RespVerbatimString.Prefix => ParseVerbatimString(buffer, ref position),
            RespArray.Prefix => ParseArray(buffer, ref position),
            _ => throw new InvalidOperationException($"Unknown RESP prefix: {prefix}")
        };
    }

    private static RespSimpleString ParseSimpleString(byte[] buffer, ref int position)
    {
        var value = ReadLine(buffer, ref position);
        return new RespSimpleString(value);
    }

    private static RespSimpleError ParseSimpleError(byte[] buffer, ref int position)
    {
        var value = ReadLine(buffer, ref position);
        return new RespSimpleError(value);
    }

    private static RespInteger ParseInteger(byte[] buffer, ref int position)
    {
        var value = ReadLine(buffer, ref position);
        return new RespInteger(long.Parse(value));
    }

    private static RespBulkString ParseBulkString(byte[] buffer, ref int position)
    {
        var lengthLine = ReadLine(buffer, ref position);
        var length = int.Parse(lengthLine);

        if (length == -1)
        {
            return new RespBulkString(null);
        }

        var value = ReadFixedLengthString(buffer, ref position, length);
        ReadLine(buffer, ref position); // Consume terminator
        return new RespBulkString(value);
    }

    private static RespBoolean ParseBoolean(byte[] buffer, ref int position)
    {
        var valueChar = (char)buffer[position++];
        ReadLine(buffer, ref position); // Consume terminator
        var value = valueChar == 't';
        return new RespBoolean(value);
    }

    private static RespDouble ParseDouble(byte[] buffer, ref int position)
    {
        var value = ReadLine(buffer, ref position);
        return new RespDouble(double.Parse(value, CultureInfo.InvariantCulture));
    }

    private static RespBigNumber ParseBigNumber(byte[] buffer, ref int position)
    {
        var value = ReadLine(buffer, ref position);
        return new RespBigNumber(value);
    }

    private static RespBulkError ParseBulkError(byte[] buffer, ref int position)
    {
        var lengthLine = ReadLine(buffer, ref position);
        var length = int.Parse(lengthLine);
        var value = ReadFixedLengthString(buffer, ref position, length);
        ReadLine(buffer, ref position); // Consume terminator
        return new RespBulkError(value);
    }

    private static RespVerbatimString ParseVerbatimString(byte[] buffer, ref int position)
    {
        var lengthLine = ReadLine(buffer, ref position);
        var length = int.Parse(lengthLine);
        var encoding = ReadFixedLengthString(buffer, ref position, 3);
        position++; // Consume ':'
        var value = ReadFixedLengthString(buffer, ref position, length - 3);
        ReadLine(buffer, ref position); // Consume terminator
        return new RespVerbatimString(encoding, value);
    }

    private static RespArray ParseArray(byte[] buffer, ref int position)
    {
        var lengthLine = ReadLine(buffer, ref position);
        var length = int.Parse(lengthLine);

        var items = new IRespData[length];
        for (var i = 0; i < length; i++)
        {
            items[i] = ParseRespData(buffer, ref position);
        }

        return new RespArray(items);
    }

    private static string ReadLine(byte[] buffer, ref int position)
    {
        var start = position;
        while (buffer[position] != '\r')
        {
            position++;
        }

        var line = Encoding.UTF8.GetString(buffer, start, position - start);
        position += 2; // Consume \r\n
        return line;
    }

    private static string ReadFixedLengthString(byte[] buffer, ref int position, int length)
    {
        var result = Encoding.UTF8.GetString(buffer, position, length);
        position += length;
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