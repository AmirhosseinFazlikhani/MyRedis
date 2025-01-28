namespace Redis.Server.Replication;

public readonly struct NodeAddress(string host, int port)
{
    public string Host { get; } = host;
    public int Port { get; } = port;
}