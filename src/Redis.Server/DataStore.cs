using System.Collections.Concurrent;
using System.Reflection;

namespace Redis.Server;

public static class DataStore
{
    public static readonly ConcurrentDictionary<string, string> KeyValueStore = new();
    public static readonly ConcurrentDictionary<string, DateTime> KeyExpiryStore = new();

    public static async Task StartDeletingExpiredKeysCycleAsync(IClock clock)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            DeleteExpiredKeys(clock);
        }
    }

    private static FieldInfo? _tablesFieldInfo;
    private static FieldInfo? _bucketsFieldInfo;
    private static FieldInfo? _nodeFieldInfo;
    private static FieldInfo? _nodeKeyFieldInfo;
    private static FieldInfo? _nodeValueFieldInfo;
    private static FieldInfo? _nodeNextFieldInfo;

    private const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void DeleteExpiredKeys(IClock clock)
    {
        _tablesFieldInfo ??= typeof(ConcurrentDictionary<string, DateTime>).GetField("_tables", bindingFlags)!;
        _bucketsFieldInfo ??= _tablesFieldInfo.FieldType.GetField("_buckets", bindingFlags)!;

        var tables = _tablesFieldInfo.GetValue(KeyExpiryStore);
        var buckets = (Array)_bucketsFieldInfo.GetValue(tables)!;

        foreach (var bucket in buckets)
        {
            _nodeFieldInfo ??= bucket.GetType().GetField("_node", bindingFlags)!;
            _nodeValueFieldInfo ??= _nodeFieldInfo.FieldType.GetField("_value", bindingFlags)!;
            _nodeKeyFieldInfo ??= _nodeFieldInfo.FieldType.GetField("_key", bindingFlags)!;
            _nodeNextFieldInfo ??= _nodeFieldInfo.FieldType.GetField("_next", bindingFlags)!;

            var node = _nodeFieldInfo.GetValue(bucket);

            while (node is not null)
            {
                var value = (DateTime)_nodeValueFieldInfo.GetValue(node)!;

                if (value < clock.Now())
                {
                    var key = (string)_nodeKeyFieldInfo.GetValue(node)!;

                    if (KeyExpiryStore.TryRemove(new KeyValuePair<string, DateTime>(key, value)))
                    {
                        KeyValueStore.TryRemove(key, out _);
                    }
                }

                node = _nodeNextFieldInfo.GetValue(node);
            }
        }
    }
}