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

    private static void ValidateSimpleText(string value)
    {
        if (value.Any(c => c is '\n' or '\r'))
        {
            throw new ArgumentException("Invalid character", nameof(value));
        }
    }
}