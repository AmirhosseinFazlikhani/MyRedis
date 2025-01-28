using Redis.Server.Protocol;

namespace Redis.Server;

public class SetCommand:ICommand
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
        switch (_options.Condition)
        {
            case SetCond.None:
                SetValue();
                break;
            case SetCond.Exists:
                if (!DataStore.ContainsKey(_options.Key, _clock))
                {
                    return new BulkStringResult(null);
                }

                SetValue();
                break;
            case SetCond.NotExists:
                if (DataStore.ContainsKey(_options.Key, _clock))
                {
                    return new BulkStringResult(null);
                }

                SetValue();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ReplyHelper.OK();
        
        void SetValue()
        {
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
            else if(DataStore.KeyExpiryStore.TryGetValue(_options.Key, out var oldExpiry) && oldExpiry < _clock.Now())
            {
                DataStore.KeyExpiryStore.Remove(_options.Key);
            }
        }
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