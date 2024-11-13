using System.Text;
using RESP.DataTypes;

namespace RESP.Tests.Unit;

public class Resp2SerializerTest
{
    [Fact]
    public void SerializeSimpleString_should_serialize_correctly()
    {
        var result = Resp2Serializer.Serialize(new RespSimpleString("OK"));
        Assert.Equal("+OK\r\n", result);
    }

    [Theory]
    [InlineData(123, "123")]
    [InlineData(-123, "-123")]
    public void SerializeInteger_should_serialize_correctly(long value, string expected)
    {
        var result = Resp2Serializer.Serialize(new RespInteger(value));
        Assert.Equal($":{expected}\r\n", result);
    }

    [Fact]
    public void SerializeSimpleError_should_serialize_correctly()
    {
        var result = Resp2Serializer.Serialize(new RespSimpleError("Unknown command!"));
        Assert.Equal("-Unknown command!\r\n", result);
    }

    [Fact]
    public void SerializeBulkString_should_serialize_correctly()
    {
        var result = Resp2Serializer.Serialize(new RespBulkString("Hello!\nHow are you?\r\n"));
        Assert.Equal("$21\r\nHello!\nHow are you?\r\n\r\n", result);
    }

    [Fact]
    public void SerializeBulkString_should_serialize_correctly_when_value_is_null()
    {
        var result = Resp2Serializer.Serialize(new RespBulkString(null));
        Assert.Equal("$-1\r\n", result);
    }

    [Theory]
    [InlineData(false, 'f')]
    [InlineData(true, 't')]
    public void SerializeBoolean_should_serialize_correctly(bool value, char expected)
    {
        var result = Resp2Serializer.Serialize(new RespBoolean(value));
        Assert.Equal($"#{expected}\r\n", result);
    }

    [Theory]
    [InlineData(123, "123")]
    [InlineData(123.456, "123.456")]
    [InlineData(-123.456, "-123.456")]
    public void SerializeDouble_should_serialize_correctly(double value, string expected)
    {
        var result = Resp2Serializer.Serialize(new RespDouble(value));
        Assert.Equal($",{expected}\r\n", result);
    }

    [Fact]
    public void SerializeBigNumber_should_serialize_correctly()
    {
        var result =
            Resp2Serializer.Serialize(new RespBigNumber("123456789123456789123456789123456789123456789123456789"));
        Assert.Equal("(123456789123456789123456789123456789123456789123456789\r\n", result);
    }

    [Fact]
    public void SerializeBulkError_should_serialize_correctly()
    {
        var result = Resp2Serializer.Serialize(new RespBulkError("Unknown command!\nStart index is out of range\r\n"));
        Assert.Equal("!46\r\nUnknown command!\nStart index is out of range\r\n\r\n", result);
    }

    [Fact]
    public void SerializeVerbatimString_should_serialize_correctly()
    {
        var result = Resp2Serializer.Serialize(new RespVerbatimString("Hello world!", "txt"));
        Assert.Equal("=12\r\ntxt:Hello world!\r\n", result);
    }

    [Fact]
    public void SerializeArray_should_serialize_correctly_when_array_is_empty()
    {
        var result = Resp2Serializer.Serialize(new RespArray([]));
        Assert.Equal("*0\r\n", result);
    }

    [Fact]
    public void SerializeArray_should_serialize_correctly()
    {
        var array = new RespArray([
            new RespSimpleString("OK"),
            new RespInteger(123),
            new RespArray([
                new RespBoolean(true),
                new RespDouble(1234)
            ]),
            new RespArray([new RespBoolean(false)])
        ]);

        var result = Resp2Serializer.Serialize(array);

        Assert.Equal("*4\r\n+OK\r\n:123\r\n*2\r\n#t\r\n,1234\r\n*1\r\n#f\r\n", result);
    }

    [Fact]
    public void Deserialize_should_deserialize_array_correctly()
    {
        var array = new RespArray([
            new RespSimpleString("OK"),
            new RespInteger(123),
            new RespArray([
                new RespBoolean(true),
                new RespDouble(1234)
            ]),
            new RespArray([new RespBoolean(false)])
        ]);

        var serializedArray = Resp2Serializer.Serialize(array);
        var arrayBytes = Encoding.UTF8.GetBytes(serializedArray);

        var result = Resp2Serializer.Deserialize(arrayBytes);

        Assert.Single(result);
        Assert.Equivalent(array, result[0]);
    }
}