using AutoFixture;
using NSubstitute;
using Redis.Server.Protocol;

namespace Redis.Server.Tests.Unit;

public class GetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_return_null_when_key_not_found()
    {
        var reply = new GetCommand(new Clock(), _fixture.Create<string>()).Execute();

        Assert.Equal(new BulkStringResult(null), reply);
    }

    [Fact]
    public void Should_return_null_when_key_has_expired()
    {
        var key = _fixture.Create<string>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        DataStore.KeyValueStore[key] = _fixture.Create<string>();
        DataStore.KeyExpiryStore[key] = clock.Now().Subtract(TimeSpan.FromSeconds(1));

        var reply = new GetCommand(clock, key).Execute();

        Assert.Equal(new BulkStringResult(null), reply);
    }

    [Fact]
    public void Should_delete_key_when_it_has_expired()
    {
        var key = _fixture.Create<string>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        DataStore.KeyValueStore[key] = _fixture.Create<string>();
        DataStore.KeyExpiryStore[key] = clock.Now().Subtract(TimeSpan.FromSeconds(1));

        new GetCommand(clock, key).Execute();

        Assert.False(DataStore.KeyValueStore.ContainsKey(key));
        Assert.False(DataStore.KeyExpiryStore.ContainsKey(key));
    }

    [Fact]
    public void Should_return_value_when_key_found_and_has_no_expiry()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();

        DataStore.KeyValueStore[key] = value;

        var reply = new GetCommand(new Clock(), key).Execute();

        Assert.Equal(new BulkStringResult(value), reply);
    }

    [Fact]
    public void Should_return_value_when_key_found_and_has_time_to_live()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        DataStore.KeyValueStore[key] = value;
        DataStore.KeyExpiryStore[key] = clock.Now().Add(TimeSpan.FromSeconds(1));

        var reply = new GetCommand(clock, key).Execute();

        Assert.Equal(new BulkStringResult(value), reply);
    }
}