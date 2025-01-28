using System.Buffers;
using System.Net.Sockets;
using System.Text;
using Redis.Server.CommandDispatching;
using Redis.Server.Protocol;

namespace Redis.Server;

public class ClientConnection : IDisposable
{
    private bool _started;
    private bool _disposed;
    private readonly TcpClient _tcpClient;
    private readonly IClock _clock;

    public ClientConnection(int clientId, TcpClient tcpClient, IClock clock)
    {
        ClientId = clientId;
        ClientName = string.Empty;
        _tcpClient = tcpClient;
        _clock = clock;
    }

    public int ClientId { get; }
    public string ClientName { get; set; }

    public async Task AcceptCommandsAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        var arrayPool = ArrayPool<byte>.Create();
        var buffer = arrayPool.Rent(256);
        var replies = new List<IResult>();

        try
        {
            while (await ReadPipeline() is { } inputs)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                var commands = inputs.Select(i => CommandFactory.Create(i, _clock, this)).ToList();
                var validCommands = new List<ICommand>();
                var errors = new List<(int index, IError error)>();

                for (var i = 0; i < commands.Count; i++)
                {
                    if (commands[i].IsSuccess)
                    {
                        validCommands.Add(commands[i].Value!);
                        continue;
                    }

                    errors.Add((i, commands[i].Error!));
                }

                await CommandSynchronizer.PostAndWaitAsync(validCommands, SaveReply);

                foreach (var (index, error) in errors)
                {
                    replies.Insert(index, error);
                }

                var serializedReplies = replies.Aggregate(string.Empty,
                    (current, reply) => current + SerializerProvider.Serializer.Serialize(reply));

                await _tcpClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializedReplies), cancellationToken);

                replies.Clear();
            }
        }
        finally
        {
            arrayPool.Return(buffer);
            Dispose();
        }

        return;

        void SaveReply(IResult reply) => replies.Add(reply);

        async Task<List<string[]>?> ReadPipeline()
        {
            try
            {
                var readBytesCount = await _tcpClient.GetStream().ReadAsync(buffer, cancellationToken);

                if (readBytesCount == 0)
                {
                    return null;
                }

                while (_tcpClient.GetStream().DataAvailable)
                {
                    var newBufferSize = buffer.Length * 2;

                    if (newBufferSize < 0)
                    {
                        newBufferSize = int.MaxValue;
                    }

                    var newBuffer = arrayPool.Rent(newBufferSize);
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    arrayPool.Return(buffer);
                    buffer = newBuffer;

                    readBytesCount += await _tcpClient.GetStream()
                        .ReadAsync(buffer.AsMemory(readBytesCount), cancellationToken);
                }

                var commands = SerializerProvider.Serializer.Deserialize(buffer[..readBytesCount]);
                return commands.Select(RespDataHelper.AsBulkStringArray).ToList();
            }
            catch (IOException e)
                when (e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
            {
                return null;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            return;
        }

        _tcpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}