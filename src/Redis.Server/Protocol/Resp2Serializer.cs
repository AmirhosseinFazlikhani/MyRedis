using System.Globalization;
using System.Text;

namespace Redis.Server.Protocol;

public class Resp2Serializer : IResultSerializer
{
    private const string Terminator = "\r\n";

    public string Serialize(IResult value)
    {
        return (string)Serialize((dynamic)value);
    }

    public List<IResult> Deserialize(byte[] value)
    {
        var result = new List<IResult>();
        var position = 0;

        while (position < value.Length)
        {
            result.Add(ParseRespData(value, ref position));
        }

        return result;
    }

    private static string Serialize(SimpleStringResult data)
    {
        ValidateSimpleText(data.Value);
        return $"{GetTypePrefix(data)}{data.Value}{Terminator}";
    }

    private static string Serialize(SimpleErrorResult data)
    {
        ValidateSimpleText(data.Value);
        return $"{GetTypePrefix(data)}{data.Value}{Terminator}";
    }

    private static string Serialize(IntegerResult data)
    {
        return $"{GetTypePrefix(data)}{data.Value}{Terminator}";
    }

    private static string Serialize(BulkStringResult data)
    {
        return data.Value is null
            ? $"{GetTypePrefix(data)}-1{Terminator}"
            : $"{GetTypePrefix(data)}{data.Value.Length}{Terminator}{data.Value}{Terminator}";
    }

    private static string Serialize(BooleanResult data)
    {
        var valueChar = data.Value ? 't' : 'f';
        return $"{GetTypePrefix(data)}{valueChar}{Terminator}";
    }

    private static string Serialize(DoubleResult data)
    {
        return $"{GetTypePrefix(data)}{data.Value}{Terminator}";
    }

    private static string Serialize(BigNumberResult data)
    {
        return $"{GetTypePrefix(data)}{data.Value}{Terminator}";
    }

    private static string Serialize(BulkErrorResult data)
    {
        return $"{GetTypePrefix(data)}{data.Value.Length}{Terminator}{data.Value}{Terminator}";
    }

    private static string Serialize(VerbatimStringResult data)
    {
        if (data.Encoding.Length != 3)
        {
            throw new ArgumentException("Encoding length should be 3", nameof(data.Encoding));
        }

        return $"{GetTypePrefix(data)}{data.Value.Length}{Terminator}{data.Encoding}:{data.Value}{Terminator}";
    }

    private static string Serialize(ArrayResult data)
    {
        if (data.Items.Length == 0)
        {
            return $"{GetTypePrefix(data)}0{Terminator}";
        }

        var serializedValue = new StringBuilder();

        foreach (var item in data.Items)
        {
            var serializedItem = (string)Serialize((dynamic)item);
            serializedValue.Append(serializedItem);
        }

        return $"{GetTypePrefix(data)}{data.Items.Length}{Terminator}{serializedValue}";
    }

    private static IResult ParseRespData(byte[] source, ref int position)
    {
        var prefix = (char)source[position++];

        return prefix switch
        {
            '+' => ParseSimpleString(source, ref position),
            '-' => ParseSimpleError(source, ref position),
            ':' => ParseInteger(source, ref position),
            '$' => ParseBulkString(source, ref position),
            '#' => ParseBoolean(source, ref position),
            ',' => ParseDouble(source, ref position),
            '(' => ParseBigNumber(source, ref position),
            '!' => ParseBulkError(source, ref position),
            '=' => ParseVerbatimString(source, ref position),
            '*' => ParseArray(source, ref position),
            _ => throw new InvalidOperationException($"Unknown RESP prefix: {prefix}")
        };
    }

    private static SimpleStringResult ParseSimpleString(byte[] source, ref int position)
    {
        var value = ReadLine(source, ref position);
        return new SimpleStringResult(value);
    }

    private static SimpleErrorResult ParseSimpleError(byte[] source, ref int position)
    {
        var value = ReadLine(source, ref position);
        return new SimpleErrorResult(value);
    }

    private static IntegerResult ParseInteger(byte[] source, ref int position)
    {
        var value = ReadLine(source, ref position);
        return new IntegerResult(long.Parse(value));
    }

    private static BulkStringResult ParseBulkString(byte[] source, ref int position)
    {
        var lengthLine = ReadLine(source, ref position);
        var length = int.Parse(lengthLine);

        if (length == -1)
        {
            return new BulkStringResult(null);
        }

        var value = ReadFixedLengthString(source, ref position, length);
        ReadLine(source, ref position); // Consume terminator
        return new BulkStringResult(value);
    }

    private static BooleanResult ParseBoolean(byte[] source, ref int position)
    {
        var valueChar = (char)source[position++];
        ReadLine(source, ref position); // Consume terminator
        var value = valueChar == 't';
        return new BooleanResult(value);
    }

    private static DoubleResult ParseDouble(byte[] source, ref int position)
    {
        var value = ReadLine(source, ref position);
        return new DoubleResult(double.Parse(value, CultureInfo.InvariantCulture));
    }

    private static BigNumberResult ParseBigNumber(byte[] source, ref int position)
    {
        var value = ReadLine(source, ref position);
        return new BigNumberResult(value);
    }

    private static BulkErrorResult ParseBulkError(byte[] source, ref int position)
    {
        var lengthLine = ReadLine(source, ref position);
        var length = int.Parse(lengthLine);
        var value = ReadFixedLengthString(source, ref position, length);
        ReadLine(source, ref position); // Consume terminator
        return new BulkErrorResult(value);
    }

    private static VerbatimStringResult ParseVerbatimString(byte[] source, ref int position)
    {
        var lengthLine = ReadLine(source, ref position);
        var length = int.Parse(lengthLine);
        var encoding = ReadFixedLengthString(source, ref position, 3);
        position++; // Consume ':'
        var value = ReadFixedLengthString(source, ref position, length - 3);
        ReadLine(source, ref position); // Consume terminator
        return new VerbatimStringResult(encoding, value);
    }

    private static ArrayResult ParseArray(byte[] source, ref int position)
    {
        var lengthLine = ReadLine(source, ref position);
        var length = int.Parse(lengthLine);

        var items = new IResult[length];
        for (var i = 0; i < length; i++)
        {
            items[i] = ParseRespData(source, ref position);
        }

        return new ArrayResult(items);
    }

    private static string ReadLine(byte[] source, ref int position)
    {
        var start = position;
        while (source[position] != '\r')
        {
            position++;
        }

        var line = Encoding.UTF8.GetString(source, start, position - start);
        position += 2; // Consume \r\n
        return line;
    }

    private static string ReadFixedLengthString(byte[] source, ref int position, int length)
    {
        var result = Encoding.UTF8.GetString(source, position, length);
        position += length;
        return result;
    }

    private static void ValidateSimpleText(string value)
    {
        if (value.Any(c => c is '\n' or '\r'))
        {
            throw new ArgumentException("Invalid character", nameof(value));
        }
    }

    // private static readonly FrozenDictionary<Type, char> _typePrefixTable = new Dictionary<Type, char>()
    //     {
    //         { typeof(SimpleStringResult), '+' },
    //         { typeof(SimpleErrorResult), '-' },
    //         { typeof(VerbatimStringResult), '=' },
    //         { typeof(MapResult), '%' },
    //         { typeof(IntegerResult), ':' },
    //         { typeof(DoubleResult), ',' },
    //         { typeof(BulkStringResult), '$' },
    //         { typeof(BulkErrorResult), '!' },
    //         { typeof(BooleanResult), '#' },
    //         { typeof(BigNumberResult), '(' },
    //         { typeof(ArrayResult), '*' },
    //     }
    //     .ToFrozenDictionary();
    //
    // private static char GetTypePrefix<T>(T value) where T : IResult => _typePrefixTable[value.GetType()];

    private static char GetTypePrefix<T>(T value) where T : IResult => value switch
    {
        SimpleStringResult => '+',
        SimpleErrorResult => '-',
        VerbatimStringResult => '=',
        MapResult => '%',
        IntegerResult => ':',
        DoubleResult => ',',
        BulkStringResult => '$',
        BulkErrorResult => '!',
        BooleanResult => '#',
        BigNumberResult => '(',
        ArrayResult => '*',
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}