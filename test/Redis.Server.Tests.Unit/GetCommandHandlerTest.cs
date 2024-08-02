using AutoFixture;
using NSubstitute;
using RESP.DataTypes;

namespace Redis.Server.Tests.Unit;

public class GetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_return_error_when_there_are_some_additional_arguments()
    {
        var reply = GetCommandHandler.Handle(["GET", "foo", "LabLabLab"], new Clock());

        Assert.Equal(new RespSimpleError("ERR wrong number of arguments for 'GET' command"), reply);
    }

    [Fact]
    public void Should_return_null_when_key_does_not_exists()
    {
        var reply = GetCommandHandler.Handle(["GET", _fixture.Create<string>()], new Clock());

        Assert.Equal(new RespBulkString(null), reply);
    }

    [Fact]
    public void Should_return_null_when_key_has_expired()
    {
        var key = _fixture.Create<string>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        DatabaseProvider.Database[key] = new Entry(_fixture.Create<string>())
        {
            Expiry = clock.Now().Subtract(TimeSpan.FromSeconds(1))
        };

        var reply = GetCommandHandler.Handle(["GET", key], new Clock());

        Assert.Equal(new RespBulkString(null), reply);
    }

    [Fact]
    public void Should_delete_key_when_it_has_expired()
    {
        var key = _fixture.Create<string>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        DatabaseProvider.Database[key] = new Entry(_fixture.Create<string>())
        {
            Expiry = clock.Now().Subtract(TimeSpan.FromSeconds(1))
        };

        GetCommandHandler.Handle(["GET", key], clock);

        Assert.False(DatabaseProvider.Database.ContainsKey(key));
    }

    [Fact]
    public void Should_return_value_when_key_found_and_has_not_expired()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        DatabaseProvider.Database[key] = new Entry(value);

        var reply = GetCommandHandler.Handle(["GET", key], new Clock());

        Assert.Equal(new RespBulkString(value), reply);
    }
}