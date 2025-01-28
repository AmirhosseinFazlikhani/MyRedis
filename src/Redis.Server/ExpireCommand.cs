using Redis.Server.Protocol;

namespace Redis.Server;

public class ExpireCommand : ICommand
{
    private readonly IClock _clock;
    private readonly ExpireOptions _options;

    public ExpireCommand(IClock clock, ExpireOptions options)
    {
        _clock = clock;
        _options = options;
    }

    public IResult Execute()
    {
        if (!DataStore.ContainsKey(_options.Key, _clock))
        {
            return new IntegerResult(0);
        }

        var expiry = _clock.Now().AddSeconds(_options.ExpirySeconds);

        if (string.IsNullOrEmpty(_options.Condition))
        {
            DataStore.KeyExpiryStore[_options.Key] = expiry;
            return new IntegerResult(1);
        }

        var hasExpiry = DataStore.KeyExpiryStore.TryGetValue(_options.Key, out var currentExpiry);

        bool shouldSetExpiry;
        switch (_options.Condition)
        {
            case "nx":
                shouldSetExpiry = !hasExpiry;
                break;
            case "xx":
                shouldSetExpiry = hasExpiry;
                break;
            case "gt":
                shouldSetExpiry = hasExpiry && currentExpiry < expiry;
                break;
            case "lt":
                shouldSetExpiry = hasExpiry && currentExpiry > expiry;
                break;
            default:
                return ReplyHelper.SyntaxError();
        }

        if (shouldSetExpiry)
        {
            DataStore.KeyExpiryStore[_options.Key] = expiry;
            return new IntegerResult(1);
        }

        return new IntegerResult(0);
    }
}

public class ExpireOptions
{
    public required string Key { get; set; }
    public required long ExpirySeconds { get; set; }
    public string? Condition { get; set; }
}