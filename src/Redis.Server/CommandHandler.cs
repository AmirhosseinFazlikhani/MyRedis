using System.Collections.Concurrent;
using Redis.Server.Persistence;
using Redis.Server.Replication;
using RESP.DataTypes;
using Serilog;

namespace Redis.Server;

public class CommandHandler : ICommandHandler, IDisposable
{
    private readonly IClock _clock;
    private readonly BlockingCollection<Command> _commandQueue = new();

    public CommandHandler(IClock clock)
    {
        _clock = clock;
        Start();
    }

    public void Handle(string[] args, Action<IRespData>? callback, ClientConnection? sender)
    {
        _commandQueue.Add(new Command(args, callback, sender));
    }

    private void Start()
    {
        Task.Factory.StartNew(() =>
            {
                foreach (var (args, callback, sender) in _commandQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var reply = HandleCommand(args, sender);
                        callback?.Invoke(reply);
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception, "An unhandled exception was thrown during handling a command");
                        var reply = new RespSimpleError("ERR internal error");
                        callback?.Invoke(reply);
                    }
                }
            },
            TaskCreationOptions.LongRunning);
    }

    private IRespData HandleCommand(string[] args, ClientConnection? client) => args[0].ToLower() switch
    {
        "ping" => PingCommandHandler.Handle(args),
        "hello" => HelloCommandHandler.Handle(args, client ?? throw new ArgumentNullException(nameof(client))),
        "get" => GetCommandHandler.Handle(args, _clock),
        "set" => SetCommandHandler.Handle(args, _clock),
        "config" => args[1].ToLower() switch
        {
            "get" => ConfigGetCommandHandler.Handle(args),
            _ => UnknownSubcommand(args[1])
        },
        "keys" => KeysCommandHandler.Handle(args, _clock),
        "expire" => ExpireCommandHandler.Handle(args, _clock),
        "client" => args[1].ToLower() switch
        {
            "setname" => ClientSetNameCommandHandler.Handle(args,
                client ?? throw new ArgumentNullException(nameof(client))),
            "getname" => ClientGetNameCommandHandler.Handle(args,
                client ?? throw new ArgumentNullException(nameof(client))),
            _ => UnknownSubcommand(args[1])
        },
        "save" => SaveCommandHandler.Handle(args, _clock),
        "bgsave" => BGSaveCommandHandler.Handle(args, _clock),
        "lastsave" => LastSaveCommandHandler.Handle(args),
        "replicaof" => ReplicaOfCommandHandler.Handle(args, _clock, this),
        _ => new RespSimpleError($"ERR unknown command '{args[0]}'")
    };

    private static RespSimpleError UnknownSubcommand(string subcommand)
    {
        return new RespSimpleError($"ERR unknown subcommand '{subcommand}'");
    }

    public void Dispose()
    {
        _commandQueue.Dispose();
    }

    private record Command(string[] args, Action<IRespData>? callback, ClientConnection? Sender);
}