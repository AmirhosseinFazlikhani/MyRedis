using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class GetCommandHandler : ICommandHandler
{
    private readonly IClock _clock;

    public GetCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public IRespData Handle(string[] parameters, RequestContext context)
    {
        if (parameters.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("GET");
        }

        if (!DatabaseProvider.Database.TryGetValue(parameters[1], out var value))
        {
            return new RespBulkString(null);
        }

        if (value.IsExpired(_clock))
        {
            DatabaseProvider.Database.Remove(parameters[1], out _);
            return new RespBulkString(null);
        }

        return new RespBulkString(value.Value);
    }
}