using RESP.DataTypes;

namespace Redis.Server;

public class PingCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        var reply = args.Length == 1 ? "PONG" : args[1];
        return new RespSimpleString(reply);
    }
}