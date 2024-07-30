using Redis.Server.CommandHandlers;
using RESP.DataTypes;

namespace Redis.Server;

public class CommandMediator
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandMediator(IClock clock, Configuration configuration)
    {
        _handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "ping", new PingCommandHandler() },
            { "hello", new HelloCommandHandler() },
            { "get", new GetCommandHandler(clock) },
            { "set", new SetCommandHandler(clock) },
            { "config", new ConfigCommandHandler(configuration) }
        };
    }

    public IRespData Send(string[] args, RequestContext context)
    {
        return _handlers.TryGetValue(args[0], out var handler)
            ? handler.Handle(args, context)
            : new RespSimpleError($"ERR unknown command '{args[0]}'");
    }
}