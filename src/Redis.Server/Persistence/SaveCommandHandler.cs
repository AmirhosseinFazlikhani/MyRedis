using RESP.DataTypes;

namespace Redis.Server.Persistence;

public class SaveCommandHandler
{
    public static IRespData Handle(string[] parameters, IClock clock)
    {
        if (parameters.Length != 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("SAVE");
        }

        if (RdbFile.SaveInProgress)
        {
            return new RespSimpleError("ERR Background save already in progress");
        }

        RdbFile.SaveAsync(clock, DataStore.KeyValueStore, DataStore.KeyExpiryStore).Wait();
        return ReplyHelper.OK();
    }
}