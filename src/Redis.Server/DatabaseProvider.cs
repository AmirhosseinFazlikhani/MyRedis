using System.Collections.Concurrent;

namespace Redis.Server;

public static class DatabaseProvider
{
    public static readonly ConcurrentDictionary<string, Entry> Database = new();
}