using AutoFixture;
using NSubstitute;
using RESP.DataTypes;

namespace Redis.Server.Tests.Unit;

public class SetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_return_error_when_there_are_some_additional_arguments()
    {
        var reply = SetCommandHandler.Handle(["SET", "foo", "bar", "EX", "10", "XX", "KEEPTTL", "LabLabLab"],
            new Clock());
        
        Assert.Equal(new RespSimpleError("ERR wrong number of arguments for 'SET' command"), reply);
    }

    [Theory]
    [InlineData("EX")]
    [InlineData("PX")]
    public void Should_return_error_when_expiry_is_not_an_integer(string expiryType)
    {
        var reply = SetCommandHandler.Handle(["SET", "foo", "bar", expiryType, "NaN"], new Clock());
        Assert.Equal(new RespSimpleError("ERR value is not an integer or out of range"), reply);
    }

    [Theory]
    [InlineData("EX")]
    [InlineData("PX")]
    public void Should_return_error_when_there_are_more_than_one_expiry(string expiryType)
    {
        var reply = SetCommandHandler.Handle(["SET", "foo", "bar", expiryType, "10", expiryType, "20"], new Clock());
        Assert.Equal(new RespSimpleError("ERR syntax error"), reply);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("NX")]
    public void Should_return_error_when_there_are_more_than_one_set_condition(string condition)
    {
        var reply = SetCommandHandler.Handle(["SET", "foo", "bar", condition, condition], new Clock());
        Assert.Equal(new RespSimpleError("ERR syntax error"), reply);
    }

    [Fact]
    public void Should_set_successfully_when_key_has_no_expiry()
    {
        var key = _fixture.Create<string>();
        var expectedValue = _fixture.Create<string>();

        var reply = SetCommandHandler.Handle(["SET", key, expectedValue], new Clock());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.Equal(expectedValue, actualValue);
        Assert.False(DataStore.KeyExpiryStore.ContainsKey(key));
    }

    [Fact]
    public void Should_set_successfully_when_key_has_expiry_in_seconds()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);
        
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var ttl = _fixture.Create<long>();
        var expectedExpiry = clock.Now().AddSeconds(ttl);

        var reply = SetCommandHandler.Handle(["SET", key, value, "EX", ttl.ToString()], clock);

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(key, out var actualExpiry));
        Assert.Equal(expectedExpiry, actualExpiry);
    }

    [Fact]
    public void Should_set_successfully_when_key_has_expiry_in_milliseconds()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);
        
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var ttl = _fixture.Create<long>();
        var expectedExpiry = clock.Now().AddMilliseconds(ttl);

        var reply = SetCommandHandler.Handle(["SET", key, value, "PX", ttl.ToString()], clock);

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(key, out var actualExpiry));
        Assert.Equal(expectedExpiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_expiry_when_key_already_exists()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);
        
        var key = _fixture.Create<string>();
        var oldValue = _fixture.Create<string>();
        var newValue = _fixture.Create<string>();
        var oldTtl = _fixture.Create<long>();
        var newTtl = _fixture.Create<long>();
        var expectedExpiry = clock.Now().AddSeconds(newTtl);

        var reply1 = SetCommandHandler.Handle(["SET", key, oldValue, "EX", oldTtl.ToString()], clock);
        var reply2 = SetCommandHandler.Handle(["SET", key, newValue, "EX", newTtl.ToString()], clock);

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(key, out var actualExpiry));
        Assert.Equal(newValue, actualValue);
        Assert.Equal(expectedExpiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_expiry_when_key_already_exists_but_has_expired()
    {
        var now = DateTime.UtcNow;
        var clock1 = Substitute.For<IClock>();
        clock1.Now().Returns(now);
        
        var key = _fixture.Create<string>();
        var oldValue = _fixture.Create<string>();
        var newValue = _fixture.Create<string>();
        var oldTtl = _fixture.Create<long>();
        var expectedTtl = _fixture.Create<long>();
        
        var clock2 = Substitute.For<IClock>();
        clock2.Now().Returns(now.AddSeconds(oldTtl + 1));
        
        var expectedExpiry = clock2.Now().AddSeconds(expectedTtl);

        var reply1 = SetCommandHandler.Handle(["SET", key, oldValue, "EX", oldTtl.ToString()], clock1);
        var reply2 = SetCommandHandler.Handle(["SET", key, newValue, "EX", expectedTtl.ToString(), "KEEPTTL"], clock2);

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(key, out var actualExpiry));
        Assert.Equal(newValue, actualValue);
        Assert.Equal(expectedExpiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_keep_expiry_when_key_already_exists_and_there_is_KEEPTTL_option()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);
        
        var key = _fixture.Create<string>();
        var oldValue = _fixture.Create<string>();
        var newValue = _fixture.Create<string>();
        var oldTtl = _fixture.Create<long>();
        var newTtl = _fixture.Create<long>();
        var expectedExpiry = clock.Now().AddSeconds(oldTtl);
        
        var reply1 = SetCommandHandler.Handle(["SET", key, oldValue, "EX", oldTtl.ToString()], clock);
        var reply2 = SetCommandHandler.Handle(["SET", key, newValue, "EX", newTtl.ToString(), "KEEPTTL"], clock);

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(key, out var actualExpiry));
        Assert.Equal(newValue, actualValue);
        Assert.Equal(expectedExpiry, actualExpiry);
    }

    [Fact]
    public void Should_return_null_when_key_is_not_exists_and_there_is_XX_option()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        var reply = SetCommandHandler.Handle(["SET", key, value, "XX"], new Clock());

        Assert.Equal(new RespBulkString(null), reply);
    }

    [Fact]
    public void Should_set_successfully_when_key_already_exists_and_there_is_XX_option()
    {
        var key = _fixture.Create<string>();
        var oldValue = _fixture.Create<string>();
        var newValue = _fixture.Create<string>();

        var reply1 = SetCommandHandler.Handle(["SET", key, oldValue], new Clock());
        var reply2 = SetCommandHandler.Handle(["SET", key, newValue, "XX"], new Clock());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.Equal(newValue, actualValue);
    }

    [Fact]
    public void Should_return_null_when_key_already_exists_and_there_is_NX_option()
    {
        var key = _fixture.Create<string>();
        var oldValue = _fixture.Create<string>();
        var newValue = _fixture.Create<string>();

        var reply1 = SetCommandHandler.Handle(["SET", key, oldValue], new Clock());
        var reply2 = SetCommandHandler.Handle(["SET", key, newValue, "NX"], new Clock());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespBulkString(null), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.Equal(oldValue, actualValue);
    }

    [Fact]
    public void Should_set_successfully_when_key_is_not_exists_and_there_is_NX_option()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        var reply = SetCommandHandler.Handle(["SET", key, value, "NX"], new Clock());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DataStore.KeyValueStore.TryGetValue(key, out var actualValue));
        Assert.Equal(value, actualValue);
    }
}