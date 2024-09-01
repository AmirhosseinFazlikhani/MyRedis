using RESP.DataTypes;

namespace Redis.Server;

public class ExpireCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock)
    {
        if (args.Length is < 3 or > 4)
        {
            return ReplyHelper.WrongArgumentsNumberError("EXPIRE");
        }
        
        var key = args[1];

        if (!long.TryParse(args[2], out var expirySeconds))
        {
            return ReplyHelper.IntegerParsingError();
        }

        if (!DataStore.ContainsKey(key, clock))
        {
            return new RespInteger(0);
        }

        var expiry = clock.Now().AddSeconds(expirySeconds);

        if (args.Length == 3)
        {
            DataStore.KeyExpiryStore[key] = expiry;
            return new RespInteger(1);
        }

        var option = args[3].ToLower();
        var hasExpiry = DataStore.KeyExpiryStore.TryGetValue(key, out var currentExpiry);

        switch (option)
        {
            case "nx":
                if (hasExpiry)
                {
                    return new RespInteger(0);
                }

                DataStore.KeyExpiryStore[key] = expiry;
                return new RespInteger(1);

            case "xx":
                if (hasExpiry)
                {
                    DataStore.KeyExpiryStore[key] = expiry;
                    return new RespInteger(1);
                }

                return new RespInteger(0);

            case "gt":
                if (hasExpiry && currentExpiry < expiry)
                {
                    DataStore.KeyExpiryStore[key] = expiry;
                    return new RespInteger(1);
                }
                
                return new RespInteger(0);

            case "lt":
                if (hasExpiry && currentExpiry > expiry)
                {
                    DataStore.KeyExpiryStore[key] = expiry;
                    return new RespInteger(1);
                }
                
                return new RespInteger(0);

            default:
                return ReplyHelper.SyntaxError();
        }
    }
}