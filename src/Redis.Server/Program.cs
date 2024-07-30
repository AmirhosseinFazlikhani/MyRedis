using System.Net;
using System.Net.Sockets;
using CommandLine;

namespace Redis.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = BuildConfiguration(args);

        if (configuration is null)
        {
            return;
        }

        TcpListener? server = null;

        try
        {
            server = new TcpListener(IPAddress.Parse(configuration.Host), configuration.Port);
            server.Start();
            Console.WriteLine("Server is now listening on {0}:{1}", configuration.Host, configuration.Port);

            var lastConnectionId = 0;
            var commandMediator = new CommandMediator(new Clock(), configuration);

            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("New connection!");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                new Connection(++lastConnectionId, client, commandMediator).StartAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server?.Stop();
        }

        Console.WriteLine("\nHit enter to continue...");
        Console.Read();
    }

    private static Configuration? BuildConfiguration(string[] args)
    {
        var options = Parser.Default.ParseArguments<InputArgs>(args);

        if (options.Errors.Any())
        {
            return null;
        }

        var configuration = new Configuration
        {
            Host = options.Value.Host ?? "127.0.0.1",
            Port = options.Value.Port ?? 6379,
            Directory = options.Value.Directory ?? Path.Combine(Path.GetTempPath(), "redis-files"),
            DbFileName = options.Value.DbFileName ?? "dump.rdb"
        };

        return configuration;
    }

    class InputArgs
    {
        [Option('p', "port")]
        public int? Port { get; set; }

        [Option('h', "host")]
        public string? Host { get; set; }

        [Option("dir")]
        public string? Directory { get; set; }

        [Option("dbfilename")]
        public string? DbFileName { get; set; }
    }
}