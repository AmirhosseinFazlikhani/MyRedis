using System.Text;
using Redis.Server.Protocol;

namespace RESP.Tests.Unit;

public class Resp2SerializerTest
{
    [Fact]
    public void SerializeSimpleString_should_serialize_correctly()
    {
        var result = new Resp2Serializer().Serialize(new SimpleStringResult("OK"));
        Assert.Equal("+OK\r\n", result);
    }

    [Theory]
    [InlineData(123, "123")]
    [InlineData(-123, "-123")]
    public void SerializeInteger_should_serialize_correctly(long value, string expected)
    {
        var result = new Resp2Serializer().Serialize(new IntegerResult(value));
        Assert.Equal($":{expected}\r\n", result);
    }

    [Fact]
    public void SerializeSimpleError_should_serialize_correctly()
    {
        var result = new Resp2Serializer().Serialize(new SimpleErrorResult("Unknown command!"));
        Assert.Equal("-Unknown command!\r\n", result);
    }

    [Fact]
    public void SerializeBulkString_should_serialize_correctly()
    {
        var result = new Resp2Serializer().Serialize(new BulkStringResult("Hello!\nHow are you?\r\n"));
        Assert.Equal("$21\r\nHello!\nHow are you?\r\n\r\n", result);
    }

    [Fact]
    public void SerializeBulkString_should_serialize_correctly_when_value_is_null()
    {
        var result = new Resp2Serializer().Serialize(new BulkStringResult(null));
        Assert.Equal("$-1\r\n", result);
    }

    [Theory]
    [InlineData(false, 'f')]
    [InlineData(true, 't')]
    public void SerializeBoolean_should_serialize_correctly(bool value, char expected)
    {
        var result = new Resp2Serializer().Serialize(new BooleanResult(value));
        Assert.Equal($"#{expected}\r\n", result);
    }

    [Theory]
    [InlineData(123, "123")]
    [InlineData(123.456, "123.456")]
    [InlineData(-123.456, "-123.456")]
    public void SerializeDouble_should_serialize_correctly(double value, string expected)
    {
        var result = new Resp2Serializer().Serialize(new DoubleResult(value));
        Assert.Equal($",{expected}\r\n", result);
    }

    [Fact]
    public void SerializeBigNumber_should_serialize_correctly()
    {
        var result =
            new Resp2Serializer().Serialize(new BigNumberResult("123456789123456789123456789123456789123456789123456789"));
        Assert.Equal("(123456789123456789123456789123456789123456789123456789\r\n", result);
    }

    [Fact]
    public void SerializeBulkError_should_serialize_correctly()
    {
        var result = new Resp2Serializer().Serialize(new BulkErrorResult("Unknown command!\nStart index is out of range\r\n"));
        Assert.Equal("!46\r\nUnknown command!\nStart index is out of range\r\n\r\n", result);
    }

    [Fact]
    public void SerializeVerbatimString_should_serialize_correctly()
    {
        var result = new Resp2Serializer().Serialize(new VerbatimStringResult("Hello world!", "txt"));
        Assert.Equal("=12\r\ntxt:Hello world!\r\n", result);
    }

    [Fact]
    public void SerializeArray_should_serialize_correctly_when_array_is_empty()
    {
        var result = new Resp2Serializer().Serialize(new ArrayResult([]));
        Assert.Equal("*0\r\n", result);
    }

    [Fact]
    public void SerializeArray_should_serialize_correctly()
    {
        var array = new ArrayResult([
            new SimpleStringResult("OK"),
            new IntegerResult(123),
            new ArrayResult([
                new BooleanResult(true),
                new DoubleResult(1234)
            ]),
            new ArrayResult([new BooleanResult(false)])
        ]);

        var result = new Resp2Serializer().Serialize(array);

        Assert.Equal("*4\r\n+OK\r\n:123\r\n*2\r\n#t\r\n,1234\r\n*1\r\n#f\r\n", result);
    }

    [Fact]
    public void Deserialize_should_deserialize_array_correctly()
    {
        var array = new ArrayResult([
            new SimpleStringResult("OK"),
            new IntegerResult(123),
            new ArrayResult([
                new BooleanResult(true),
                new DoubleResult(1234)
            ]),
            new ArrayResult([new BooleanResult(false)])
        ]);

        var serializedArray = new Resp2Serializer().Serialize(array);
        var arrayBytes = Encoding.UTF8.GetBytes(serializedArray);

        var result = new Resp2Serializer().Deserialize(arrayBytes);

        Assert.Single(result);
        Assert.Equivalent(array, result[0]);
    }
}