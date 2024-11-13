namespace Redis.Server;

public interface ICommandConsumer
{
    void Add(string[] args, ClientConnection? sender = null);
}