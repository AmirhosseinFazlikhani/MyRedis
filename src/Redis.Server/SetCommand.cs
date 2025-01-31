using Redis.Server.Protocol;

namespace Redis.Server;

public class SetCommand : ICommand
{
    private readonly IClock _clock;
    private readonly SetOptions _options;

    public SetCommand(IClock clock, SetOptions options)
    {
        _clock = clock;
        _options = options;
    }

    public IResult Execute()
    {
        var conditionPassed = _options.Condition switch
        {
            SetCond.Exists => DataStore.ContainsKey(_options.Key, _clock),
            SetCond.NotExists => !DataStore.ContainsKey(_options.Key, _clock),
            SetCond.None => true,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!conditionPassed)
        {
            return new BulkStringResult(null);
        }

        DataStore.KeyValueStore[_options.Key] = _options.Value;

        if (!_options.KeepTtl)
        {
            if (_options.Expiry.HasValue)
            {
                DataStore.KeyExpiryStore[_options.Key] = _options.Expiry.Value;
            }
            else
            {
                DataStore.KeyExpiryStore.Remove(_options.Key);
            }
        }
        else if (!DataStore.IsKeyLive(_options.Key, _clock))
        {
            DataStore.KeyExpiryStore.Remove(_options.Key);
        }

        return ReplyHelper.OK();
    }
}

public class SetOptions
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTime? Expiry { get; set; }
    public bool KeepTtl { get; set; }
    public SetCond Condition { get; set; } = SetCond.None;
}

public enum SetCond
{
    None,
    Exists,
    NotExists
}