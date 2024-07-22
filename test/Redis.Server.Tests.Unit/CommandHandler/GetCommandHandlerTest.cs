using AutoFixture;
using NSubstitute;
using Redis.Server.CommandHandlers;
using RESP.DataTypes;

namespace Redis.Server.Tests.Unit.CommandHandler;

public class GetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_return_error_when_there_are_some_additional_arguments()
    {
        var reply = new GetCommandHandler(new Clock())
            .Handle(["GET", "foo", "LabLabLab"], new RequestContext());

        Assert.Equal(new RespSimpleError("ERR wrong number of arguments for 'GET' command"), reply);
    }

    [Fact]
    public void Should_return_null_when_key_does_not_exists()
    {
        var reply = new GetCommandHandler(new Clock())
            .Handle(["GET", _fixture.Create<string>()], new RequestContext());

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
            Expiry = clock.Now().Subtract(TimeSpan.FromMilliseconds(1))
        };
        
        var reply = new GetCommandHandler(clock)
            .Handle(["GET", key], new RequestContext());

        Assert.Equal(new RespBulkString(null), reply);
    }

    [Fact]
    public void Should_return_value_when_key_found_and_has_not_expired()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        DatabaseProvider.Database[key] = new Entry(value);
        
        var reply = new GetCommandHandler(new Clock())
            .Handle(["GET", key], new RequestContext());

        Assert.Equal(new RespBulkString(value), reply);
    }
}