using Redis.Server.Protocol;

namespace Redis.Server;

public interface ICommand
{
    IResult Execute();
}