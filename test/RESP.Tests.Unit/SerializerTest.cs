﻿using System.Text;

namespace RESP.Tests.Unit;

public class SerializerTest
{
    [Fact]
    public void SerializeSimpleString_should_serialize_correctly()
    {
        var result = Serializer.SerializeSimpleString("OK");

        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("+OK\r\n", decodedResult);
    }

    [Theory]
    [InlineData(123, "123")]
    [InlineData(-123, "-123")]
    public void SerializeInteger_should_serialize_correctly(long value, string expected)
    {
        var result = Serializer.SerializeInteger(value);

        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal($":{expected}\r\n", decodedResult);
    }

    [Fact]
    public void SerializeSimpleError_should_serialize_correctly()
    {
        var result = Serializer.SerializeSimpleError("Unknown command!");
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("-Unknown command!\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeBulkString_should_serialize_correctly()
    {
        var result = Serializer.SerializeBulkString("Hello!\nHow are you?\r\n");
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("$21\r\nHello!\nHow are you?\r\n\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeNull_should_serialize_correctly()
    {
        var result = Serializer.SerializeNull();
    
        Assert.Equal("_\r\n"u8.ToArray(), result);
    }
    
    [Theory]
    [InlineData(false, 'f')]
    [InlineData(true, 't')]
    public void SerializeBoolean_should_serialize_correctly(bool value, char expected)
    {
        var result = Serializer.SerializeBoolean(value);
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal($"#{expected}\r\n", decodedResult);
    }
    
    [Theory]
    [InlineData(123, "123")]
    [InlineData(123.456, "123.456")]
    [InlineData(-123.456, "-123.456")]
    public void SerializeDouble_should_serialize_correctly(double value, string expected)
    {
        var result = Serializer.SerializeDouble(value);
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal($",{expected}\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeBigNumber_should_serialize_correctly()
    {
        var result = Serializer.SerializeBigNumber("123456789123456789123456789123456789123456789123456789");
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("(123456789123456789123456789123456789123456789123456789\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeBulkError_should_serialize_correctly()
    {
        var result = Serializer.SerializeBulkError("Unknown command!\nStart index is out of range\r\n");
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("!46\r\nUnknown command!\nStart index is out of range\r\n\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeVerbatimString_should_serialize_correctly()
    {
        var result = Serializer.SerializeVerbatimString("Hello world!", "txt");
    
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("=12\r\ntxt:Hello world!\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeArray_should_serialize_correctly_when_array_is_empty()
    {
        var result = Serializer.SerializeArray([]);
        
        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("*0\r\n", decodedResult);
    }
    
    [Fact]
    public void SerializeArray_should_serialize_correctly()
    {
        var array = new[]
        {
            Serializer.SerializeSimpleString("OK"),
            Serializer.SerializeInteger(123),
            Serializer.SerializeArray([Serializer.SerializeBoolean(true), Serializer.SerializeDouble(1234)]),
            Serializer.SerializeArray([Serializer.SerializeBoolean(false)])
        };
    
        var result = Serializer.SerializeArray(array);

        var decodedResult = Encoding.UTF8.GetString(result);
        Assert.Equal("*4\r\n+OK\r\n:123\r\n*2\r\n#t\r\n,1234\r\n*1\r\n#f\r\n", decodedResult);
    }
}