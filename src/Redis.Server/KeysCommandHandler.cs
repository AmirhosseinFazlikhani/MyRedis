using GlobExpressions;
using RESP.DataTypes;

namespace Redis.Server;

public class KeysCommandHandler
{
    public static IRespData Handle(string[] parameters)
    {
        if (parameters.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("KEYS");
        }

        var keys = DataStore.KeyValueStore.Keys.Where(k => Glob.IsMatch(k, parameters[1]))
            .Select(k => new RespBulkString(k) as IRespData)
            .ToArray();

        return new RespArray(keys);
    }
}