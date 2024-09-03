namespace Redis.Server;

public interface ICommandConsumer
{
    void Add(string[] args, Client client);
}