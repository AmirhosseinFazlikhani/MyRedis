using RESP.DataTypes;

namespace Redis.Server;

public class BGSaveCommandHandler
{
    public static IRespData Handle(string[] parameters, IClock clock)
    {
        if (parameters.Length != 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("SAVE");
        }

        if (Persistence.SaveInProgress)
        {
            return new RespSimpleError("ERR Background save already in progress");
        }

        var keyValueStoreSnapshot = DataStore.KeyValueStore.ToDictionary();
        var keyExpiryStoreSnapshot = DataStore.KeyExpiryStore.ToDictionary();
        Task.Run(() => Persistence.Save(clock, keyValueStoreSnapshot, keyExpiryStoreSnapshot));

        return ReplyHelper.OK();
    }
}