using Redis.Server.Protocol;

namespace Redis.Server.Persistence;

public class SaveCommand : ICommand
{
    private readonly IClock _clock;

    public SaveCommand(IClock clock)
    {
        _clock = clock;
    }

    public IResult Execute()
    {
        if (RdbFile.SaveInProgress)
        {
            return new SimpleErrorResult("ERR Background save already in progress");
        }

        RdbFile.SaveAsync(_clock, DataStore.KeyValueStore, DataStore.KeyExpiryStore).Wait();
        return ReplyHelper.OK();
    }
}