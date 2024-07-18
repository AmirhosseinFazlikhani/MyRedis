using System.Collections.Concurrent;
using RESP;

namespace Redis.Server;

public static class CommandHandler
{
    private const int ProtoVersion = 2;
    private static readonly ConcurrentDictionary<string, Entry> _database = new();

    public static string Handle(string[] parameters, int connectionId)
    {
        if (parameters[0].Equals("ping", StringComparison.OrdinalIgnoreCase))
        {
            var reply = parameters.Length == 1 ? "PONG" : parameters[1];
            return Serializer.SerializeSimpleString(reply);
        }

        if (parameters[0].Equals("hello", StringComparison.OrdinalIgnoreCase))
        {
            if (parameters.Length == 2 && parameters[1] != ProtoVersion.ToString())
            {
                return Serializer.SerializeSimpleError("NOPROTO sorry, this protocol version is not supported.");
            }

            return Serializer.SerializeArray([
                Serializer.SerializeSimpleString("server"),
                Serializer.SerializeSimpleString("redis"),
                Serializer.SerializeSimpleString("version"),
                Serializer.SerializeSimpleString("7.0.0"),
                Serializer.SerializeSimpleString("proto"),
                Serializer.SerializeInteger(ProtoVersion),
                Serializer.SerializeSimpleString("id"),
                Serializer.SerializeInteger(connectionId),
                Serializer.SerializeSimpleString("mode"),
                Serializer.SerializeSimpleString("standalone"),
                Serializer.SerializeSimpleString("role"),
                Serializer.SerializeSimpleString("master"),
                Serializer.SerializeSimpleString("modules"),
                Serializer.SerializeArray([]),
            ]);
        }

        if (parameters[0].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            var entry = new Entry(parameters[2]);

            var options = parameters.AsSpan(3..);
            var optionsCount = options.Length;

            if (optionsCount > 5)
            {
                return WrongArgumentsNumberError("set");
            }

            var setCond = SetCond.None;
            var keepTtl = false;

            var currentOptionIndex = 0;
            while (currentOptionIndex < options.Length)
            {
                if (options[currentOptionIndex].Equals("ex", StringComparison.OrdinalIgnoreCase))
                {
                    currentOptionIndex++;

                    if (!long.TryParse(options[currentOptionIndex], out var seconds))
                    {
                        return IntegerParsingError();
                    }

                    if (entry.Expiry.HasValue)
                    {
                        return SyntaxError();
                    }

                    entry.Expiry = DateTime.UtcNow.AddSeconds(seconds);
                }
                else if (options[currentOptionIndex].Equals("px", StringComparison.OrdinalIgnoreCase))
                {
                    currentOptionIndex++;

                    if (!long.TryParse(options[currentOptionIndex], out var milliseconds))
                    {
                        return IntegerParsingError();
                    }

                    if (entry.Expiry.HasValue)
                    {
                        return SyntaxError();
                    }

                    entry.Expiry = DateTime.UtcNow.AddMilliseconds(milliseconds);
                }
                else if (options[currentOptionIndex].Equals("xx", StringComparison.OrdinalIgnoreCase))
                {
                    if (setCond != SetCond.None)
                    {
                        return SyntaxError();
                    }

                    setCond = SetCond.Exists;
                }
                else if (options[currentOptionIndex].Equals("nx", StringComparison.OrdinalIgnoreCase))
                {
                    if (setCond != SetCond.None)
                    {
                        return SyntaxError();
                    }

                    setCond = SetCond.NotExists;
                }
                else if (options[currentOptionIndex].Equals("keepttl", StringComparison.OrdinalIgnoreCase))
                {
                    keepTtl = true;
                }
                else
                {
                    return SyntaxError();
                }

                currentOptionIndex++;
            }

            switch (setCond)
            {
                case SetCond.None:
                    SetValue(parameters[1], entry, keepTtl);
                    break;
                case SetCond.Exists:
                    lock (parameters[1])
                    {
                        if (!_database.TryGetValue(parameters[1], out var value) || value.IsExpired())
                        {
                            return Serializer.SerializeBulkString(null);
                        }

                        SetValue(parameters[1], entry, keepTtl);
                    }

                    break;
                case SetCond.NotExists:
                    lock (parameters[1])
                    {
                        if (_database.TryGetValue(parameters[1], out var value) && !value.IsExpired())
                        {
                            return Serializer.SerializeBulkString(null);
                        }

                        SetValue(parameters[1], entry, keepTtl);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return OK();
        }

        if (parameters[0].Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            if (parameters.Length > 2)
            {
                return WrongArgumentsNumberError("get");
            }

            _database.TryGetValue(parameters[1], out var value);
            return Serializer.SerializeBulkString(value?.GetValue());
        }

        return Serializer.SerializeSimpleError($"ERR unknown command '{parameters[0]}'");
    }

    private static void SetValue(string key, Entry entry, bool keepTtl)
    {
        if (keepTtl)
        {
            _database.AddOrUpdate(key, entry, (_, e) =>
            {
                entry.Expiry = e.Expiry;
                return entry;
            });
        }
        else
        {
            _database[key] = entry;
        }
    }

    private static string OK()
    {
        return Serializer.SerializeSimpleString("OK");
    }

    private static string IntegerParsingError()
    {
        return Serializer.SerializeSimpleString("ERR value is not an integer or out of range");
    }

    private static string WrongArgumentsNumberError(string command)
    {
        return Serializer.SerializeSimpleError($"ERR wrong number of arguments for '{command}' command");
    }

    private static string SyntaxError()
    {
        return Serializer.SerializeSimpleError("ERR syntax error");
    }
}