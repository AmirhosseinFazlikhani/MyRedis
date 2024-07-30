namespace Redis.Server;

public class Configuration
{
    public required int Port { get; init; }

    public required string Host { get; init; }

    public required string Directory { get; init; }

    public required string DbFileName { get; init; }
}