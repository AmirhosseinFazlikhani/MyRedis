using RESP.DataTypes;

namespace Redis.Server;

public class DelCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        if (args.Length == 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("DEL");
        }

        var deletedKeysCount = 0;

        for (var i = 1; i < args.Length; i++)
        {
            var key = args[i];
            if (!DataStore.KeyValueStore.Remove(key)) continue;
            deletedKeysCount++;
            DataStore.KeyExpiryStore.Remove(key);
        }

        return new RespInteger(deletedKeysCount);
    }
}