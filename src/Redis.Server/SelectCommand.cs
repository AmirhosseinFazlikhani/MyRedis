using Redis.Server.Protocol;

namespace Redis.Server;

public class SelectCommand : ICommand
{
    private readonly string _dbNumber;

    public SelectCommand(string dbNumber)
    {
        _dbNumber = dbNumber;
    }

    public IResult Execute()
    {
        if (_dbNumber != "0")
        {
            return new SimpleErrorResult("ERR DB index is out of range");
        }

        return ReplyHelper.OK();
    }
}