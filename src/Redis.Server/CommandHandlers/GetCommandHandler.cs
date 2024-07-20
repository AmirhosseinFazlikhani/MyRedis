using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class GetCommandHandler : ICommandHandler
{
    public IRespData Handle(string[] parameters, RequestContext context)
    {
        if (parameters.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("get");
        }

        DatabaseProvider.Database.TryGetValue(parameters[1], out var value);
        return new RespBulkString(value?.GetValue());
    }
}