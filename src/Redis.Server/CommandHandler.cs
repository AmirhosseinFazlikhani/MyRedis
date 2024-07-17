using RESP;

namespace Redis.Server;

public static class CommandHandler
{
    private const int ProtoVersion = 2;

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

        return Serializer.SerializeSimpleError($"ERR unknown command '{parameters[0]}'");
    }
}