using Redis.Server.Protocol;

namespace Redis.Server;

public interface IScope
{
    IResultSerializer Serializer { get; set; }
    ClientConnection Client { get; }
}

public class Scope : IScope
{
    private readonly ClientConnection? _client;

    public ClientConnection Client
    {
        get => _client ?? throw new NotSupportedException();
        init => _client = value;
    }

    public IResultSerializer Serializer { get; set; } = SerializerProvider.DefaultSerializer;
}