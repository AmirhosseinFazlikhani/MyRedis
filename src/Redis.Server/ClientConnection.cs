using System.Buffers;
using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class ClientConnection : IDisposable
{
    private bool _started;
    private bool _disposed;
    private readonly TcpClient _tcpClient;
    private int _unhandledCommandsCount;
    private readonly List<IRespData> _bufferedReplies = [];
    private readonly SemaphoreSlim _semaphore = new(0);

    public ClientConnection(int clientId, TcpClient tcpClient)
    {
        ClientId = clientId;
        ClientName = string.Empty;
        _tcpClient = tcpClient;
    }
    
    public int ClientId { get; }
    public string ClientName { get; set; }

    public async Task AcceptCommandsAsync(ICommandHandler commandConsumer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        var arrayPool = ArrayPool<byte>.Create();
        var buffer = arrayPool.Rent(256);

        try
        {
            while (await ReadPipeline() is { } commands)
            {
                _unhandledCommandsCount = commands.Count;
                
                foreach (var command in commands)
                {
                    commandConsumer.Handle(command, Reply, this);
                }

                await _semaphore.WaitAsync();

                var serializedReplies = _bufferedReplies.Aggregate(string.Empty,
                    (current, reply) => current + (string)Resp2Serializer.Serialize((dynamic)reply));

                await _tcpClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializedReplies));

                _bufferedReplies.Clear();
            }
        }
        finally
        {
            arrayPool.Return(buffer);
            Dispose();
        }

        return;

        async Task<List<string[]>?> ReadPipeline()
        {
            try
            {
                var readBytesCount = await _tcpClient.GetStream().ReadAsync(buffer);

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
                        .ReadAsync(buffer.AsMemory(readBytesCount));
                }

                var commands = Resp2Serializer.Deserialize(buffer[..readBytesCount]);
                return commands.Select(RespDataHelper.AsBulkStringArray).ToList();
            }
            catch (IOException e)
                when (e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
            {
                return null;
            }
        }
    }

    private void Reply(IRespData data)
    {
        _bufferedReplies.Add(data);
        _unhandledCommandsCount--;

        if (_unhandledCommandsCount == 0)
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            return;
        }

        _tcpClient.Dispose();
        _semaphore.Dispose();
        _disposed = true;
    }
}