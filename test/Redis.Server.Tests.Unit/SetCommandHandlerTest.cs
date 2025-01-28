using AutoFixture;
using NSubstitute;
using Redis.Server.Protocol;

namespace Redis.Server.Tests.Unit;

public class SetCommandHandlerTest
{
    private static readonly Fixture _fixture = new();

    [Fact]
    public void Should_set_successfully_when_key_has_no_expiry()
    {
        var options = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
        };

        var reply = new SetCommand(new Clock(), options).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options.Key, out var actualValue));
        Assert.Equal(options.Value, actualValue);
        Assert.False(DataStore.KeyExpiryStore.ContainsKey(options.Key));
    }

    [Fact]
    public void Should_set_successfully_when_key_has_expiry()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var options = _fixture.Create<SetOptions>();
        options.KeepTtl = false;
        options.Condition = SetCond.None;
        options.Expiry = clock.Now().AddSeconds(_fixture.Create<int>());

        var reply = new SetCommand(clock, options).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply);
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(options.Key, out var actualExpiry));
        Assert.Equal(options.Expiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_expiry_when_key_already_exists()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var options1 = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
            Expiry = clock.Now().AddMilliseconds(_fixture.Create<int>())
        };
        
        var options2 = new SetOptions
        {
            Key = options1.Key,
            Value = _fixture.Create<string>(),
            Expiry = clock.Now().AddMilliseconds(_fixture.Create<int>())
        };

        var reply1 = new SetCommand(clock, options1).Execute();
        var reply2 = new SetCommand(clock, options2).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply1);
        Assert.Equal(new SimpleStringResult("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options1.Key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(options1.Key, out var actualExpiry));
        Assert.Equal(options2.Value, actualValue);
        Assert.Equal(options2.Expiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_expiry_when_key_already_exists_but_has_expired()
    {
        var clock1 = Substitute.For<IClock>();
        clock1.Now().Returns(DateTime.UtcNow);

        var options1 = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
            Expiry = clock1.Now().AddMilliseconds(_fixture.Create<int>())
        };

        var clock2 = Substitute.For<IClock>();
        clock2.Now().Returns(options1.Expiry.Value.AddSeconds(1));

        var options2 = new SetOptions
        {
            Key = options1.Key,
            Value = _fixture.Create<string>(),
            Expiry = clock2.Now().AddMilliseconds(_fixture.Create<int>())
        };

        var reply1 = new SetCommand(clock1, options1).Execute();
        var reply2 = new SetCommand(clock2, options2).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply1);
        Assert.Equal(new SimpleStringResult("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options1.Key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(options1.Key, out var actualExpiry));
        Assert.Equal(options2.Value, actualValue);
        Assert.Equal(options2.Expiry, actualExpiry);
    }

    [Fact]
    public void Should_update_value_and_keep_expiry_when_key_already_exists_and_there_is_KEEPTTL_option()
    {
        var clock = Substitute.For<IClock>();
        clock.Now().Returns(DateTime.UtcNow);

        var options1 = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
            Expiry = clock.Now().AddMilliseconds(_fixture.Create<int>())
        };

        var options2 = new SetOptions
        {
            Key = options1.Key,
            Value = _fixture.Create<string>(),
            Expiry = clock.Now().AddMilliseconds(_fixture.Create<int>()),
            KeepTtl = true
        };

        var reply1 = new SetCommand(clock, options1).Execute();
        var reply2 = new SetCommand(clock, options2).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply1);
        Assert.Equal(new SimpleStringResult("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options1.Key, out var actualValue));
        Assert.True(DataStore.KeyExpiryStore.TryGetValue(options1.Key, out var actualExpiry));
        Assert.Equal(options2.Value, actualValue);
        Assert.Equal(options1.Expiry, actualExpiry);
    }

    [Fact]
    public void Should_return_null_when_key_is_not_exists_and_there_is_XX_option()
    {
        var options = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
            Condition = SetCond.Exists
        };

        var reply = new SetCommand(new Clock(), options).Execute();

        Assert.Equal(new BulkStringResult(null), reply);
    }

    [Fact]
    public void Should_set_successfully_when_key_already_exists_and_there_is_XX_option()
    {
        var options1 = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>()
        };

        var options2 = new SetOptions
        {
            Key = options1.Key,
            Value = _fixture.Create<string>(),
            Condition = SetCond.Exists
        };

        var reply1 = new SetCommand(new Clock(), options1).Execute();
        var reply2 = new SetCommand(new Clock(), options2).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply1);
        Assert.Equal(new SimpleStringResult("OK"), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options1.Key, out var actualValue));
        Assert.Equal(options2.Value, actualValue);
    }

    [Fact]
    public void Should_return_null_when_key_already_exists_and_there_is_NX_option()
    {
        var options1 = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>()
        };

        var options2 = new SetOptions
        {
            Key = options1.Key,
            Value = _fixture.Create<string>(),
            Condition = SetCond.NotExists
        };

        var reply1 = new SetCommand(new Clock(), options1).Execute();
        var reply2 = new SetCommand(new Clock(), options2).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply1);
        Assert.Equal(new BulkStringResult(null), reply2);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options1.Key, out var actualValue));
        Assert.Equal(options1.Value, actualValue);
    }

    [Fact]
    public void Should_set_successfully_when_key_is_not_exists_and_there_is_NX_option()
    {
        var options = new SetOptions
        {
            Key = _fixture.Create<string>(),
            Value = _fixture.Create<string>(),
            Condition = SetCond.NotExists
        };

        var reply = new SetCommand(new Clock(), options).Execute();

        Assert.Equal(new SimpleStringResult("OK"), reply);
        Assert.True(DataStore.KeyValueStore.TryGetValue(options.Key, out var actualValue));
        Assert.Equal(options.Value, actualValue);
    }
}