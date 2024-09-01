using RESP.DataTypes;

namespace Redis.Server;

public class GetCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock)
    {
        if (args.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("GET");
        }

        var key = args[1];

        if (!DataStore.KeyValueStore.TryGetValue(key, out var value))
        {
            return new RespBulkString(null);
        }

        if (DataStore.KeyExpiryStore.TryGetValue(key, out var expiry) && expiry < clock.Now())
        {
            DataStore.KeyValueStore.Remove(key);
            DataStore.KeyExpiryStore.Remove(key);
            return new RespBulkString(null);
        }

        return new RespBulkString(value);
    }
}