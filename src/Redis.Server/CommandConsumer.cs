using System.Collections.Concurrent;
using RESP.DataTypes;

namespace Redis.Server;

public class CommandConsumer : ICommandConsumer, IDisposable
{
    private readonly IClock _clock;
    private readonly BlockingCollection<(string[] args, ClientConnection client)> _commandQueue = new();
    private readonly BlockingCollection<(IRespData data, ClientConnection client)> _replyQueue = new();
    
    public CommandConsumer(IClock clock)
    {
        _clock = clock;
        Start();
    }

    public void Add(string[] args, ClientConnection client)
    {
        _commandQueue.Add((args, client));
    }

    private bool _disposed;
    
    private void Start()
    {
        Task.Run(async () =>
        {
            foreach (var (data, client) in _replyQueue.GetConsumingEnumerable())
            {
                await client.ReplyAsync(data);
            }
        });

        Task.Factory.StartNew(() =>
            {
                foreach (var (args, client) in _commandQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var reply = HandleCommand(args, client);
                        _replyQueue.Add((reply, client));
                    }
                    catch
                    {
                        var reply = new RespSimpleError("ERR internal error");
                        _replyQueue.Add((reply, client));
                    }
                }
                
                _replyQueue.CompleteAdding();
            },
            TaskCreationOptions.LongRunning);
    }

    private IRespData HandleCommand(string[] args, ClientConnection client) => args[0].ToLower() switch
    {
        "ping" => PingCommandHandler.Handle(args),
        "hello" => HelloCommandHandler.Handle(args, client),
        "get" => GetCommandHandler.Handle(args, _clock),
        "set" => SetCommandHandler.Handle(args, _clock),
        "config" => ConfigCommandHandler.Handle(args),
        "keys" => KeysCommandHandler.Handle(args),
        "expire" => ExpireCommandHandler.Handle(args, _clock),
        _ => new RespSimpleError($"ERR unknown command '{args[0]}'")
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _commandQueue.Dispose();
        _replyQueue.Dispose();
        _disposed = true;
    }
}