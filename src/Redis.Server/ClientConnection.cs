using System.Buffers;
using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class ClientConnection : IDisposable
{
    public readonly int ClientId;
    public string ClientName;
    private bool _started;
    private bool _disposed;
    private readonly TcpClient _tcpClient;
    private readonly ICommandConsumer _commandConsumer;
    private readonly SemaphoreSlim _semaphore = new(0);
    private int _sentCommandsCount = 0;
    private readonly List<IRespData> _replyQueue = new();
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

    public ClientConnection(int clientId,
        TcpClient tcpClient,
        ICommandConsumer commandConsumer)
    {
        ClientId = clientId;
        ClientName = string.Empty;
        _tcpClient = tcpClient;
        _commandConsumer = commandConsumer;
    }

    public async Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        var buffer = _arrayPool.Rent(256);

        try
        {
            while (await ReadPipeline() is { } commands)
            {
                _sentCommandsCount += commands.Count;
                
                foreach (var command in commands)
                {
                    _commandConsumer.Add(command, this);
                }

                await _semaphore.WaitAsync();

                var serializedReplies = _replyQueue.Aggregate(string.Empty,
                    (current, reply) => current + (string)Resp2Serializer.Serialize((dynamic)reply));

                await _tcpClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializedReplies));

                _sentCommandsCount = 0;
                _replyQueue.Clear();
            }
        }
        finally
        {
            _arrayPool.Return(buffer);
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

                    var newBuffer = _arrayPool.Rent(newBufferSize);
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    _arrayPool.Return(buffer);
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

    public void Reply(IRespData data)
    {
        _replyQueue.Add(data);

        if (_sentCommandsCount == _replyQueue.Count)
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
        _disposed = true;
    }
}