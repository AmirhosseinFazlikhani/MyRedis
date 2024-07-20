using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public interface ICommandHandler
{
    IRespData Handle(string[] parameters, RequestContext context);
}