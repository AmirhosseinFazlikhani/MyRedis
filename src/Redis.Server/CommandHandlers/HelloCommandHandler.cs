using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class HelloCommandHandler : ICommandHandler
{
    private const int ProtoVersion = 2;

    public IRespData Handle(string[] parameters, RequestContext context)
    {
        if (parameters.Length == 2 && parameters[1] != ProtoVersion.ToString())
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
            new RespInteger(context.ConnectionId),
            new RespSimpleString("mode"),
            new RespSimpleString("standalone"),
            new RespSimpleString("role"),
            new RespSimpleString("master"),
            new RespSimpleString("modules"),
            new RespArray([])
        ]);
    }
}