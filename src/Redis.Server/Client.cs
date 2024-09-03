using System.Buffers;
using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class Client : IDisposable
{
    public Client(int id,
        TcpClient tcpClient,
        ICommandConsumer commandConsumer)
    {
        Id = id;
        _tcpClient = tcpClient;
        _commandConsumer = commandConsumer;
    }

    public int Id { get; }

    private bool _started;
    private bool _disposed;
    private readonly TcpClient _tcpClient;
    private readonly ICommandConsumer _commandConsumer;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        if (_started)
        {
            throw new NotSupportedException("Already started");
        }

        _started = true;

        Task.Run(async () =>
        {
            int readBytesCount;
            var buffer = ArrayPool<byte>.Shared.Rent(1024);

            try
            {
                while ((readBytesCount = await _tcpClient.GetStream().ReadAsync(buffer)) != 0)
                {
                    if (buffer[0] != '*')
                    {
                        throw new ProtocolErrorException();
                    }

                    var current = 1;
                    while (buffer[current] != '\r')
                    {
                        current++;
                    }

                    var argsCount = short.Parse(buffer.AsSpan(1..current));
                    current += 2;
                    var args = new string[argsCount];

                    while (argsCount > 0)
                    {
                        args[^argsCount] = ReadBulkString(ref current);
                        argsCount--;
                    }

                    _commandConsumer.Add(args, this);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                Dispose();
            }

            return;

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
        });
    }

    public async Task ReplyAsync(IRespData data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var serializedData = (string)Resp2Serializer.Serialize((dynamic)data);
        await _tcpClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializedData));
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