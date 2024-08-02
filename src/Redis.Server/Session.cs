using RESP.DataTypes;

namespace Redis.Server;

public class Session
{
    public int Id { get; }
    public DateTime CreatedAt { get; }

    private readonly ICommandMediator _commandMediator;
    private readonly ICommandStream _commandStream;

    public Session(int id,
        IClock clock,
        ICommandMediator commandMediator,
        ICommandStream commandStream)
    {
        Id = id;
        CreatedAt = clock.Now();
        _commandMediator = commandMediator;
        _commandStream = commandStream;
    }

    private bool IsStarted;

    public async Task StartAsync()
    {
        if (IsStarted)
        {
            throw new SessionAlreadyStartedException();
        }

        IsStarted = true;

        try
        {
            await foreach (var args in _commandStream.ListenAsync())
            {
                var reply = _commandMediator.Send(args, this);
                await _commandStream.ReplyAsync(reply);
            }
        }
        catch (ProtocolErrorException)
        {
            await _commandStream.ReplyAsync(new RespSimpleError("ERR Protocol error"));
        }
    }
}