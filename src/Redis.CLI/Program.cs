using System.Net.Sockets;
using System.Text;
using RESP;

namespace Redis.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        var port = 5000;
        var host = "127.0.0.1";

        var argCursor = 0;

        while (argCursor < args.Length)
        {
            switch (args[argCursor])
            {
                case "-p":
                    argCursor++;
                    port = int.Parse(args[argCursor]);
                    break;
                case "-h":
                    argCursor++;
                    host = args[argCursor];
                    break;
                default:
                    Console.WriteLine("Undefined argument: {0}", args[argCursor]);
                    return;
            }
        }

        var client = new TcpClient(host, port);
        var stream = client.GetStream();

        while (true)
        {
            Console.Write("> ");
            var command = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            var parameters = command.Split(' ').Select(Serializer.SerializeBulkString).ToArray();
            var request = Serializer.SerializeArray(parameters);

            await stream.WriteAsync(Encoding.UTF8.GetBytes(request));
            var response = await GetResponseAsync(stream);
            Console.WriteLine(response);
        }
    }

    private static async Task<string> GetResponseAsync(NetworkStream stream)
    {
        var buffer = new byte[256];
        var length = await stream.ReadAsync(buffer);
        return Encoding.UTF8.GetString(buffer[..length]);
    }
}