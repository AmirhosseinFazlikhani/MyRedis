using Redis.Server.Protocol;

namespace Redis.Server;

public class ClientGetNameCommand : ICommand
{
    private readonly ClientConnection _clientConnection;

    public ClientGetNameCommand(ClientConnection clientConnection)
    {
        _clientConnection = clientConnection;
    }

    public IResult Execute()
    {
        return new BulkStringResult(_clientConnection.ClientName);
    }
}