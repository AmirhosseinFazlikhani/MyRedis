using System.Collections.Concurrent;
using RESP;

namespace Redis.Server;

public static class CommandHandler
{
    private const int ProtoVersion = 2;
    private static readonly ConcurrentDictionary<string, string> _database = new();

    public static string Handle(string[] parameters, int connectionId)
    {
        if (CommandEquals(parameters[0], "ping"))
        {
            var reply = parameters.Length == 1 ? "PONG" : parameters[1];
            return Serializer.SerializeSimpleString(reply);
        }

        if (CommandEquals(parameters[0], "hello"))
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

        if (CommandEquals(parameters[0], "set"))
        {
            if (parameters.Length > 3)
            {
                return WrongArgumentsNumberError("set");
            }

            _database[parameters[1]] = parameters[2];
            return OK();
        }

        if (CommandEquals(parameters[0], "get"))
        {
            if (parameters.Length > 2)
            {
                return WrongArgumentsNumberError("get");
            }

            _ = _database.TryGetValue(parameters[1], out var value);
            return Serializer.SerializeBulkString(value);
        }

        return Serializer.SerializeSimpleError($"ERR unknown command '{parameters[0]}'");
    }

    private static bool CommandEquals(string input, string target)
    {
        return input.Equals(target, StringComparison.OrdinalIgnoreCase);
    }

    private static string OK()
    {
        return Serializer.SerializeSimpleString("OK");
    }

    private static string WrongArgumentsNumberError(string command)
    {
        return Serializer.SerializeSimpleError($"ERR wrong number of arguments for '{command}' command");
    }
}