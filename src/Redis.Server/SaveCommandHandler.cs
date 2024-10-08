﻿using RESP.DataTypes;

namespace Redis.Server;

public class SaveCommandHandler
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

        Persistence.Save(clock, DataStore.KeyValueStore, DataStore.KeyExpiryStore);
        return ReplyHelper.OK();
    }
}