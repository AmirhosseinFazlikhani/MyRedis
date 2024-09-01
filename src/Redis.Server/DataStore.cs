namespace Redis.Server;

public static class DataStore
{
    public static readonly object Lock = new();
    public static readonly Dictionary<string, string> KeyValueStore = new();
    public static readonly Dictionary<string, DateTime> KeyExpiryStore = new();

    public static bool ContainsKey(string key, IClock clock)
    {
        return KeyValueStore.ContainsKey(key) &&
            (!KeyExpiryStore.TryGetValue(key, out var currentExpiry) || currentExpiry >= clock.Now());
    }
}