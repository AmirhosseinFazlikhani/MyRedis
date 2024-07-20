using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class PingCommandHandler : ICommandHandler
{
    public IRespData Handle(string[] parameters, RequestContext context)
    {
        var reply = parameters.Length == 1 ? "PONG" : parameters[1];
        return new RespSimpleString(reply);
    }
}