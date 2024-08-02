using RESP.DataTypes;

namespace Redis.Server;

public class HelloCommandHandler
{
    private const int ProtoVersion = 2;

    public static IRespData Handle(string[] args, Session session)
    {
        if (args.Length == 2 && args[1] != ProtoVersion.ToString())
        {
            return new RespSimpleError("NOPROTO sorry, this protocol version is not supported.");
        }

        return new RespArray([
            new RespSimpleString("server"),
            new RespSimpleString("redis"),
            new RespSimpleString("version"),
            new RespSimpleString("7.0.0"),
            new RespSimpleString("proto"),
            new RespInteger(ProtoVersion),
            new RespSimpleString("id"),
            new RespInteger(session.Id),
            new RespSimpleString("mode"),
            new RespSimpleString("standalone"),
            new RespSimpleString("role"),
            new RespSimpleString("master"),
            new RespSimpleString("modules"),
            new RespArray([])
        ]);
    }
}