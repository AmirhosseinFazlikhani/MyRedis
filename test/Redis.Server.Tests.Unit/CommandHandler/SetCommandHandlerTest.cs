using AutoFixture;
using NSubstitute;
using Redis.Server.CommandHandlers;
using RESP.DataTypes;

namespace Redis.Server.Tests.Unit.CommandHandler;

public class SetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_return_error_when_there_are_some_additional_arguments()
    {
        var reply = new SetCommandHandler(new Clock())
            .Handle(["SET", "foo", "bar", "EX", "10", "XX", "KEEPTTL", "LabLabLab"], new RequestContext());

        Assert.Equal(new RespSimpleError("ERR wrong number of arguments for 'SET' command"), reply);
    }

    [Theory]
    [InlineData("EX")]
    [InlineData("PX")]
    public void Should_return_error_when_expiry_is_not_an_integer(string expiryType)
    {
        var reply = new SetCommandHandler(new Clock())
            .Handle(["SET", "foo", "bar", expiryType, "NaN"], new RequestContext());

        Assert.Equal(new RespSimpleError("ERR value is not an integer or out of range"), reply);
    }

    [Theory]
    [InlineData("EX")]
    [InlineData("PX")]
    public void Should_return_error_when_there_are_more_than_one_expiry(string expiryType)
    {
        var reply = new SetCommandHandler(new Clock())
            .Handle(["SET", "foo", "bar", expiryType, "10", expiryType, "20"], new RequestContext());

        Assert.Equal(new RespSimpleError("ERR syntax error"), reply);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("NX")]
    public void Should_return_error_when_there_are_more_than_one_set_condition(string condition)
    {
        var reply = new SetCommandHandler(new Clock())
            .Handle(["SET", "foo", "bar", condition, condition], new RequestContext());

        Assert.Equal(new RespSimpleError("ERR syntax error"), reply);
    }

    [Fact]
    public void Should_set_successfully_when_key_has_no_expiry()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        var reply = new SetCommandHandler(new Clock()).Handle(["SET", key, value], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value, entry.Value);
        Assert.Null(entry.Expiry);
    }

    [Fact]
    public void Should_set_successfully_when_key_has_expiry_in_seconds()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var expiry = _fixture.Create<long>();

        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var reply = new SetCommandHandler(clock).Handle(["SET", key, value, "EX", expiry.ToString()],
            new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(clock.Now().AddSeconds(expiry), entry.Expiry);
    }

    [Fact]
    public void Should_set_successfully_when_key_has_expiry_in_milliseconds()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var expiry = _fixture.Create<long>();

        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var reply = new SetCommandHandler(clock).Handle(["SET", key, value, "PX", expiry.ToString()],
            new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(clock.Now().AddMilliseconds(expiry), entry.Expiry);
    }

    [Fact]
    public void Should_update_value_and_expiry_when_key_already_exists()
    {
        var key = _fixture.Create<string>();
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<string>();
        var expiry1 = _fixture.Create<long>();
        var expiry2 = _fixture.Create<long>();

        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var commandHandler = new SetCommandHandler(clock);
        var reply1 = commandHandler.Handle(["SET", key, value1, "EX", expiry1.ToString()], new RequestContext());
        var reply2 = commandHandler.Handle(["SET", key, value2, "EX", expiry2.ToString()], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value2, entry.Value);
        Assert.Equal(clock.Now().AddSeconds(expiry2), entry.Expiry);
    }

    [Fact]
    public void Should_update_value_and_keep_expiry_when_key_already_exists_and_there_is_KEEPTTL_option()
    {
        var key = _fixture.Create<string>();
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<string>();
        var expiry1 = _fixture.Create<long>();
        var expiry2 = _fixture.Create<long>();

        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var commandHandler = new SetCommandHandler(clock);
        var reply1 = commandHandler.Handle(["SET", key, value1, "EX", expiry1.ToString()], new RequestContext());
        var reply2 = commandHandler.Handle(["SET", key, value2, "EX", expiry2.ToString(), "KEEPTTL"], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value2, entry.Value);
        Assert.Equal(clock.Now().AddSeconds(expiry1), entry.Expiry);
    }

    [Fact]
    public void Should_return_null_when_key_is_not_exists_and_there_is_XX_option()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        var reply = new SetCommandHandler(new Clock()).Handle(["SET", key, value, "XX"], new RequestContext());

        Assert.Equal(new RespBulkString(null), reply);
    }

    [Fact]
    public void Should_set_successfully_when_key_already_exists_and_there_is_XX_option()
    {
        var key = _fixture.Create<string>();
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<string>();

        var reply1 = new SetCommandHandler(new Clock()).Handle(["SET", key, value1], new RequestContext());
        var reply2 = new SetCommandHandler(new Clock()).Handle(["SET", key, value2, "XX"], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespSimpleString("OK"), reply2);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value2, entry.Value);
    }

    [Fact]
    public void Should_return_null_when_key_already_exists_and_there_is_NX_option()
    {
        var key = _fixture.Create<string>();
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<string>();

        var reply1 = new SetCommandHandler(new Clock()).Handle(["SET", key, value1], new RequestContext());
        var reply2 = new SetCommandHandler(new Clock()).Handle(["SET", key, value2, "NX"], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply1);
        Assert.Equal(new RespBulkString(null), reply2);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value1, entry.Value);
    }

    [Fact]
    public void Should_set_successfully_when_key_is_not_exists_and_there_is_NX_option()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        var reply = new SetCommandHandler(new Clock()).Handle(["SET", key, value, "NX"], new RequestContext());

        Assert.Equal(new RespSimpleString("OK"), reply);
        Assert.True(DatabaseProvider.Database.TryGetValue(key, out var entry));
        Assert.Equal(value, entry.Value);
    }
}