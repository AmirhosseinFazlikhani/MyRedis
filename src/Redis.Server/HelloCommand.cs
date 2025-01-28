using Redis.Server.Protocol;

namespace Redis.Server;

public class HelloCommand : ICommand
{
    private readonly ClientConnection _clientConnection;
    private readonly int? _requestedProtocolVersion;
    private const int CurrentProtoVersion = 2;

    public HelloCommand(ClientConnection clientConnection)
    {
        _clientConnection = clientConnection;
    }
    
    public HelloCommand(ClientConnection clientConnection, int requestedProtocolVersion)
    {
        _clientConnection = clientConnection;
        _requestedProtocolVersion = requestedProtocolVersion;
    }

    public IResult Execute()
    {
        if (_requestedProtocolVersion.HasValue && _requestedProtocolVersion != CurrentProtoVersion)
        {
            return new SimpleErrorResult("NOPROTO sorry, this protocol version is not supported.");
        }

        return new ArrayResult([
            new SimpleStringResult("server"),
            new SimpleStringResult("redis"),
            new SimpleStringResult("version"),
            new SimpleStringResult("7.0.0"),
            new SimpleStringResult("proto"),
            new IntegerResult(CurrentProtoVersion),
            new SimpleStringResult("id"),
            new IntegerResult(_clientConnection.ClientId),
            new SimpleStringResult("mode"),
            new SimpleStringResult("standalone"),
            new SimpleStringResult("role"),
            new SimpleStringResult("master"),
            new SimpleStringResult("modules"),
            new ArrayResult([])
        ]);
    }
}