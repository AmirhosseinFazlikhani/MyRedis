using RESP.DataTypes;

namespace Redis.Server;

public interface ICommandHandler
{
    void Handle(string[] args, Action<IRespData>? callback = null, ClientConnection? sender = null);
}