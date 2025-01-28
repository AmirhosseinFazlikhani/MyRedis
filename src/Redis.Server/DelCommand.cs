using Redis.Server.Protocol;

namespace Redis.Server;

public class DelCommand : ICommand
{
    private readonly string[] _keys;

    public DelCommand(string[] keys)
    {
        _keys = keys;
    }

    public IResult Execute()
    {
        var deletedKeysCount = 0;

        foreach (var key in _keys)
        {
            if (!DataStore.KeyValueStore.Remove(key)) continue;
            deletedKeysCount++;
            DataStore.KeyExpiryStore.Remove(key);
        }

        return new IntegerResult(deletedKeysCount);
    }
}