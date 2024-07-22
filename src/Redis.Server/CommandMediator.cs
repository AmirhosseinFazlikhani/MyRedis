using Redis.Server.CommandHandlers;
using RESP.DataTypes;

namespace Redis.Server;

public static class CommandMediator
{
    private static readonly Dictionary<string, ICommandHandler> _handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ping", new PingCommandHandler() },
        { "hello", new HelloCommandHandler() },
        { "get", new GetCommandHandler(new Clock()) },
        { "set", new SetCommandHandler(new Clock()) },
    };

    public static IRespData Send(string[] parameters, RequestContext context)
    {
        return _handlers.TryGetValue(parameters[0], out var handler)
            ? handler.Handle(parameters, context)
            : new RespSimpleError($"ERR unknown command '{parameters[0]}'");
    }
}