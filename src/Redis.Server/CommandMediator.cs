using RESP.DataTypes;

namespace Redis.Server;

public class CommandMediator : ICommandMediator
{
    private readonly IClock _clock;

    public CommandMediator(IClock clock)
    {
        _clock = clock;
    }

    public IRespData Send(string[] args, Session session)
    {
        lock (DataStore.Lock)
        {
            try
            {
                return args[0].ToLower() switch
                {
                    "ping" => PingCommandHandler.Handle(args),
                    "hello" => HelloCommandHandler.Handle(args, session),
                    "get" => GetCommandHandler.Handle(args, _clock),
                    "set" => SetCommandHandler.Handle(args, _clock),
                    "config" => ConfigCommandHandler.Handle(args),
                    "keys" => KeysCommandHandler.Handle(args),
                    _ => new RespSimpleError($"ERR unknown command '{args[0]}'")
                };
            }
            catch
            {
                return new RespSimpleError("ERR internal error");
            }
        }
    }
}