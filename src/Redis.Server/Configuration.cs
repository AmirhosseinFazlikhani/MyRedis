namespace Redis.Server;

public class Configuration
{
    public static int Port { get; set; } = 6379;

    public static string Host { get; set; } = "127.0.0.1";

    public static string Directory { get; set; } = Path.Combine(Path.GetTempPath(), "redis-files");

    public static string DbFileName { get; set; } = "dump.rdb";
}