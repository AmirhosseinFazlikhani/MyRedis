using RESP.DataTypes;

namespace Redis.Server;

public class GetCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock)
    {
        if (args.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("GET");
        }

        if (!DatabaseProvider.Database.TryGetValue(args[1], out var value))
        {
            return new RespBulkString(null);
        }

        if (value.IsExpired(clock))
        {
            DatabaseProvider.Database.Remove(args[1], out _);
            return new RespBulkString(null);
        }

        return new RespBulkString(value.Value);
    }
}