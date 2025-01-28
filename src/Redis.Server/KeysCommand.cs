using GlobExpressions;
using Redis.Server.Protocol;

namespace Redis.Server;

public class KeysCommand : ICommand
{
    private readonly IClock _clock;
    private readonly string _pattern;

    public KeysCommand(IClock clock, string pattern)
    {
        _clock = clock;
        _pattern = pattern;
    }

    public IResult Execute()
    {
        var keys = DataStore.KeyValueStore.Keys.Where(k => DataStore.IsKeyLive(k, _clock))
            .Where(k => Glob.IsMatch(k, _pattern))
            .Select(k => new BulkStringResult(k) as IResult)
            .ToArray();

        return new ArrayResult(keys);
    }
}