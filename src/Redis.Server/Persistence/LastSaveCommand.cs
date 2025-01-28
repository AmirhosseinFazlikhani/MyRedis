using Redis.Server.Protocol;

namespace Redis.Server.Persistence;

public class LastSaveCommand : ICommand
{
    public IResult Execute()
    {
        var value = RdbFile.LastSaveDateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
        return new IntegerResult((long)value);
    }
}