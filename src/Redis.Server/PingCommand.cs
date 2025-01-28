using Redis.Server.Protocol;

namespace Redis.Server;

public class PingCommand : ICommand
{
    private readonly string _expectedReply;

    public PingCommand(string expectedReply = "PONG")
    {
        _expectedReply = expectedReply;
    }

    public IResult Execute()
    {
        return new SimpleStringResult(_expectedReply);
    }
}