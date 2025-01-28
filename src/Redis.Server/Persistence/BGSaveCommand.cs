using Redis.Server.Protocol;

namespace Redis.Server.Persistence;

public class BGSaveCommand : ICommand
{
    private readonly IClock _clock;

    public BGSaveCommand(IClock clock)
    {
        _clock = clock;
    }

    public IResult Execute()
    {
        if (RdbFile.SaveInProgress)
        {
            return new SimpleErrorResult("ERR Background save already in progress");
        }

        var keyValueStoreSnapshot = DataStore.KeyValueStore.ToDictionary();
        var keyExpiryStoreSnapshot = DataStore.KeyExpiryStore.ToDictionary();
        RdbFile.SaveAsync(_clock, keyValueStoreSnapshot, keyExpiryStoreSnapshot);

        return ReplyHelper.OK();
    }
}