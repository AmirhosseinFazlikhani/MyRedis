using Redis.Server.Protocol;

namespace Redis.Server;

public class ClientSetNameCommand : ICommand
{
    private readonly ClientConnection _clientConnection;
    private readonly string _clientName;

    public ClientSetNameCommand(ClientConnection clientConnection, string clientName)
    {
        _clientConnection = clientConnection;
        _clientName = clientName;
    }

    public IResult Execute()
    {
        _clientConnection.ClientName = _clientName;
        return ReplyHelper.OK();
    }
}