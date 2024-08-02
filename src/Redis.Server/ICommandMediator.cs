using RESP.DataTypes;

namespace Redis.Server;

public interface ICommandMediator
{
    IRespData Send(string[] args, Session session);
}