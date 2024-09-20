using RESP.DataTypes;

namespace Redis.Server;

public class SaveCommandHandler
{
    public static IRespData Handle(string[] parameters, IClock clock)
    {
        if (parameters.Length != 1)
        {
            return ReplyHelper.WrongArgumentsNumberError("SAVE");
        }

        Persistence.Save(clock, DataStore.KeyValueStore, DataStore.KeyExpiryStore);
        return ReplyHelper.OK();
    }
}