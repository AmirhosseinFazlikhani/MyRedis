using System.Text;

namespace RESP.Tests.Unit;

public class SerializerTest
{
    [Fact]
    public void SerializeSimpleString_should_serialize_correctly()
    {
        const string value = "OK";
        var result = Serializer.SerializeSimpleString(value);

        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal($"+{value}\r\n", decodedResult);
    }

    [Fact]
    public void SerializeInteger_should_serialize_correctly()
    {
        const int value = 123;
        var result = Serializer.SerializeInteger(value);

        Assert.Equal(7, result.Length);
        Assert.Equal((byte)':', result[0]);
        Assert.Equal(value, BitConverter.ToInt32(result.AsSpan()[1..5]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeSimpleError_should_serialize_correctly()
    {
        const string value = "Unknown command!";
        var result = Serializer.SerializeSimpleError(value);

        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal($"-{value}\r\n", decodedResult);
    }

    [Fact]
    public void SerializeBulkString_should_serialize_correctly()
    {
        const string value = "Hello!\nHow are you?\r\n";
        var result = Serializer.SerializeBulkString(value);

        Assert.Equal(9 + value.Length, result.Length);
        Assert.Equal((byte)'$', result[0]);
        Assert.Equal(BitConverter.GetBytes((uint)value.Length), result[1..5]);
        Assert.Equal("\r\n"u8.ToArray(), result[5..7]);
        Assert.Equal(value, Encoding.UTF8.GetString(result[7..^2]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeNull_should_serialize_correctly()
    {
        var result = Serializer.SerializeNull();

        Assert.Equal("_\r\n"u8.ToArray(), result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SerializeBoolean_should_serialize_correctly(bool value)
    {
        var result = Serializer.SerializeBoolean(value);

        Assert.Equal(4, result.Length);
        Assert.Equal((byte)'#', result[0]);
        Assert.Equal(value, BitConverter.ToBoolean(result.AsSpan()[1..2]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeDouble_should_serialize_correctly()
    {
        const double value = 123.456;
        var result = Serializer.SerializeDouble(value);

        Assert.Equal(11, result.Length);
        Assert.Equal((byte)',', result[0]);
        Assert.Equal(value, BitConverter.ToDouble(result.AsSpan()[1..9]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeBigNumber_should_serialize_correctly()
    {
        const long value = 123456789123456789;
        var result = Serializer.SerializeBigNumber(value);

        Assert.Equal(11, result.Length);
        Assert.Equal((byte)'(', result[0]);
        Assert.Equal(value, BitConverter.ToInt64(result.AsSpan()[1..9]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeBulkError_should_serialize_correctly()
    {
        const string value = "Unknown command!\nStart index is out of range\r\n";
        var result = Serializer.SerializeBulkError(value);

        Assert.Equal(9 + value.Length, result.Length);
        Assert.Equal((byte)'!', result[0]);
        Assert.Equal(BitConverter.GetBytes((uint)value.Length), result[1..5]);
        Assert.Equal("\r\n"u8.ToArray(), result[5..7]);
        Assert.Equal(value, Encoding.UTF8.GetString(result[7..^2]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeVerbatimString_should_serialize_correctly()
    {
        const string value = "Hello world!";
        const string encoding = "txt";
        var result = Serializer.SerializeVerbatimString(value, encoding);

        Assert.Equal(13 + value.Length, result.Length);
        Assert.Equal((byte)'=', result[0]);
        Assert.Equal(BitConverter.GetBytes((uint)value.Length), result[1..5]);
        Assert.Equal("\r\n"u8.ToArray(), result[5..7]);
        Assert.Equal(encoding, Encoding.UTF8.GetString(result[7..10]));
        Assert.Equal((byte)':', result[10]);
        Assert.Equal(value, Encoding.UTF8.GetString(result[11..^2]));
        Assert.Equal("\r\n"u8.ToArray(), result[^2..]);
    }

    [Fact]
    public void SerializeArray_should_serialize_correctly()
    {
        var array = new[]
        {
            Serializer.SerializeSimpleString("OK"),
            Serializer.SerializeInteger(123),
            Serializer.SerializeBulkString("Hello world\r\n"),
            Serializer.SerializeArray([Serializer.SerializeBoolean(true), Serializer.SerializeDouble(1234)])
        };

        var result = Serializer.SerializeArray(array);
    }
}