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
    private readonly ICommandConsumer _commandConsumer;
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly List<string[]> _commandQueue = new();
    private readonly List<IRespData> _replyQueue = new();
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();

    public ClientConnection(int clientId,
        TcpClient tcpClient,
        ICommandConsumer commandConsumer)
    {
        ClientId = clientId;
        _tcpClient = tcpClient;
        _commandConsumer = commandConsumer;
    }

    public int ClientId { get; }

    public async Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        int offset;
        var buffer = _arrayPool.Rent(256);

        try
        {
            while (await ReadPipeline())
            {
                foreach (var command in _commandQueue)
                {
                    _commandConsumer.Add(command, this);
                }

                await _semaphore.WaitAsync();

                var serializedReplies = _replyQueue.Aggregate(string.Empty,
                    (current, reply) => current + (string)Resp2Serializer.Serialize((dynamic)reply));

                await _tcpClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializedReplies));

                _commandQueue.Clear();
                _replyQueue.Clear();
            }
        }
        finally
        {
            _arrayPool.Return(buffer);
            Dispose();
        }

        return;

        async Task<bool> ReadPipeline()
        {
            offset = 0;

            try
            {
                var readBytesCount = await _tcpClient.GetStream().ReadAsync(buffer);

                if (readBytesCount == 0)
                {
                    return false;
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

                while (offset < readBytesCount)
                {
                    var args = ReadStringArray();
                    _commandQueue.Add(args);
                }

                return true;
            }
            catch (IOException e)
                when (e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
            {
                return false;
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }

        string[] ReadStringArray()
        {
            if (buffer[offset] != RespArray.Prefix)
            {
                throw new ProtocolException();
            }

            offset++;
            var argsCountStartIndex = offset;

            while (buffer[offset] != '\r')
            {
                offset++;
            }

            var argsCount = short.Parse(buffer.AsSpan(argsCountStartIndex..offset));
            offset += 2;
            var args = new string[argsCount];

            while (argsCount > 0)
            {
                args[^argsCount] = ReadBulkString();
                argsCount--;
            }

            return args;
        }

        string ReadBulkString()
        {
            if (buffer[offset] != RespBulkString.Prefix)
            {
                throw new ProtocolException();
            }

            offset++;
            var sizeStartIndex = offset;

            while (buffer[offset] != '\r')
            {
                offset++;
            }

            var stringSize = int.Parse(buffer.AsSpan(sizeStartIndex..offset));
            offset += Resp2Serializer.TerminatorBytes.Length;

            if (stringSize == 0)
            {
                offset += Resp2Serializer.TerminatorBytes.Length;
                return string.Empty;
            }

            var result = Encoding.UTF8.GetString(buffer.AsSpan(offset, stringSize));
            offset += stringSize + Resp2Serializer.TerminatorBytes.Length;
            return result;
        }
    }

    public void Reply(IRespData data)
    {
        _replyQueue.Add(data);

        if (_commandQueue.Count == _replyQueue.Count)
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