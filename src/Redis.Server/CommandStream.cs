using System.Net.Sockets;
using System.Text;
using RESP;
using RESP.DataTypes;

namespace Redis.Server;

public class CommandStream : ICommandStream
{
    private readonly NetworkStream _stream;
    private readonly byte[] _buffer = new byte[256];
    private int _readBytesCount;

    public CommandStream(NetworkStream stream)
    {
        _stream = stream;
    }

    public async IAsyncEnumerable<string[]> ListenAsync()
    {
        while ((_readBytesCount = await _stream.ReadAsync(_buffer)) != 0)
        {
            if (_buffer[0] != '*')
            {
                throw new ProtocolErrorException();
            }

            var current = 1;
            while (_buffer[current] != '\r')
            {
                current++;
            }

            var argsCount = short.Parse(_buffer.AsSpan(1..current));
            current += 2;
            var args = new string[argsCount];

            while (argsCount > 0)
            {
                args[^argsCount] = ReadBulkString(ref current);
                argsCount--;
            }

            yield return args;
        }
    }

    public async Task ReplyAsync(IRespData value)
    {
        var serializedReply = (string)Resp2Serializer.Serialize((dynamic)value);
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(serializedReply));
    }

    private string ReadBulkString(ref int offset)
    {
        if (offset == _readBytesCount)
        {
            offset = 0;
            _readBytesCount = _stream.Read(_buffer);
        }

        var chunk = _buffer.AsSpan(offset.._readBytesCount);

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
        _ = _stream.Read(result, chunk.Length - current, remainingBytesCount);
        offset = _readBytesCount = _buffer.Length - 1;

        return Encoding.UTF8.GetString(result);
    }
}