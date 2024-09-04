using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class ClientConnection : IDisposable
{
    public ClientConnection(int clientId,
        TcpClient tcpClient,
        ICommandConsumer commandConsumer)
    {
        ClientId = clientId;
        _tcpClient = tcpClient;
        _commandConsumer = commandConsumer;
    }

    public int ClientId { get; }

    private bool _started;
    private bool _disposed;
    private readonly TcpClient _tcpClient;
    private readonly ICommandConsumer _commandConsumer;
    private readonly SemaphoreSlim _semaphore = new(0);

    private readonly List<string[]> _commandQueue = new();
    private readonly List<IRespData> _replyQueue = new();

    public async Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        int readBytesCount;
        var buffer = new byte[1024];

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
            Dispose();
        }

        return;

        async Task<bool> ReadPipeline()
        {
            while ((readBytesCount = await _tcpClient.GetStream().ReadAsync(buffer)) != 0)
            {
                var offset = 0;

                while (offset < readBytesCount)
                {
                    var args = ReadCommand(ref offset);
                    _commandQueue.Add(args);
                }

                if (_tcpClient.GetStream().DataAvailable)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        string[] ReadCommand(ref int offset)
        {
            if (buffer[offset] != '*')
            {
                throw new ProtocolErrorException();
            }

            offset++;
            var argsCountOffset = offset;

            while (buffer[offset] != '\r')
            {
                offset++;
            }

            var argsCount = short.Parse(buffer.AsSpan(argsCountOffset..offset));
            offset += 2;
            var args = new string[argsCount];

            while (argsCount > 0)
            {
                args[^argsCount] = ReadBulkString(ref offset);
                argsCount--;
            }

            return args;
        }

        string ReadBulkString(ref int offset)
        {
            if (offset == readBytesCount)
            {
                offset = 0;
                readBytesCount = _tcpClient.GetStream().Read(buffer);
            }

            var chunk = buffer.AsSpan(offset..readBytesCount);

            var current = 1;
            while (chunk[current] != '\r')
            {
                current++;
            }

            var size = int.Parse(chunk[1..current]);
            current += 2;
            var totalSize = size + Resp2Serializer.TerminatorBytes.Length;

            if (totalSize <= chunk.Length - current)
            {
                offset += totalSize + current;
                return Encoding.UTF8.GetString(chunk[current..(size + current)]);
            }

            var result = new byte[size];
            chunk[current..].CopyTo(result);
            var remainingBytesCount = totalSize - chunk.Length - current;
            _ = _tcpClient.GetStream().Read(result, chunk.Length - current, remainingBytesCount);
            offset = readBytesCount = buffer.Length - 1;

            return Encoding.UTF8.GetString(result);
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