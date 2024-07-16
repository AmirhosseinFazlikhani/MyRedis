using RESP;

namespace Redis.Server;

public static class CommandHandler
{
    public static Task<string> HandleAsync(string[] parameters)
    {
        foreach (var parameter in parameters)
        {
            Console.WriteLine(parameter);
        }

        return Task.FromResult(Serializer.SerializeSimpleString("OK"));
    }
}