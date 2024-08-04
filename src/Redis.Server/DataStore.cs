using System.Collections.Concurrent;

namespace Redis.Server;

public static class DataStore
{
    public static readonly ConcurrentDictionary<string, string> KeyValueStore = new();
    public static readonly ConcurrentDictionary<string, DateTime> KeyExpiryStore = new();
}