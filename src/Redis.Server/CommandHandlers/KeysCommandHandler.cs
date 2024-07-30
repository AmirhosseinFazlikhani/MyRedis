using GlobExpressions;
using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class KeysCommandHandler : ICommandHandler
{
    public IRespData Handle(string[] parameters, RequestContext context)
    {
        if (parameters.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("KEYS");
        }

        var keys = DatabaseProvider.Database.Keys.Where(k => Glob.IsMatch(k, parameters[1]))
            .Select(k => new RespBulkString(k) as IRespData)
            .ToArray();

        return new RespArray(keys);
    }
}