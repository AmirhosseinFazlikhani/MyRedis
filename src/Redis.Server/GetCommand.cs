using Redis.Server.Protocol;

namespace Redis.Server;

public class GetCommand : ICommand
{
    private readonly IClock _clock;
    private readonly string _key;

    public GetCommand(IClock clock, string key)
    {
        _clock = clock;
        _key = key;
    }

    public IResult Execute()
    {
        if (!DataStore.KeyValueStore.TryGetValue(_key, out var value))
        {
            return new BulkStringResult(null);
        }

        if (DataStore.KeyExpiryStore.TryGetValue(_key, out var expiry) && expiry < _clock.Now())
        {
            DataStore.KeyValueStore.Remove(_key);
            DataStore.KeyExpiryStore.Remove(_key);
            return new BulkStringResult(null);
        }

        return new BulkStringResult(value);
    }
}