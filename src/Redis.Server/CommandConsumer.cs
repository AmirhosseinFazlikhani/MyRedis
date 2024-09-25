using System.Collections.Concurrent;
using RESP.DataTypes;

namespace Redis.Server;

public class CommandConsumer : ICommandConsumer, IDisposable
{
    private readonly IClock _clock;
    private readonly BlockingCollection<(string[] args, ClientConnection sender)> _commandQueue = new();

    public CommandConsumer(IClock clock)
    {
        _clock = clock;
        Start();
    }

    public void Add(string[] args, ClientConnection sender)
    {
        _commandQueue.Add((args, sender));
    }

    private void Start()
    {
        Task.Factory.StartNew(() =>
            {
                foreach (var (args, client) in _commandQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var reply = HandleCommand(args, client);
                        client.Reply(reply);
                    }
                    catch
                    {
                        var reply = new RespSimpleError("ERR internal error");
                        client.Reply(reply);
                    }
                }
            },
            TaskCreationOptions.LongRunning);
    }

    private IRespData HandleCommand(string[] args, ClientConnection client) => args[0].ToLower() switch
    {
        "ping" => PingCommandHandler.Handle(args),
        "hello" => HelloCommandHandler.Handle(args, client),
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
            "setname" => ClientSetNameCommandHandler.Handle(args, client),
            "getname" => ClientGetNameCommandHandler.Handle(args, client),
            _ => UnknownSubcommand(args[1])
        },
        "save" => SaveCommandHandler.Handle(args, _clock),
        "bgsave" => BGSaveCommandHandler.Handle(args, _clock),
        "lastsave" => LastSaveCommandHandler.Handle(args),
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
}