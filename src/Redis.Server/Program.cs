using System.Net;
using System.Net.Sockets;
using CommandLine;

namespace Redis.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var parsedArgs = Parser.Default.ParseArguments<InputArgs>(args);

        if (parsedArgs.Errors.Any())
        {
            return;
        }

        if (parsedArgs.Value.Host is not null)
        {
            Configuration.Host = parsedArgs.Value.Host;
        }

        if (parsedArgs.Value.Port is not null)
        {
            Configuration.Port = parsedArgs.Value.Port.Value;
        }

        if (parsedArgs.Value.Directory is not null)
        {
            Configuration.Directory = parsedArgs.Value.Directory;
        }

        if (parsedArgs.Value.DbFileName is not null)
        {
            Configuration.DbFileName = parsedArgs.Value.DbFileName;
        }

        TcpListener? server = null;

        try
        {
            var clock = new Clock();
            Persistence.Load(clock);
            
            server = new TcpListener(IPAddress.Parse(Configuration.Host), Configuration.Port);
            server.Start();
            Console.WriteLine("Server is now listening on {0}:{1}", Configuration.Host, Configuration.Port);
            
            using var commandMediator = new CommandConsumer(clock);

            var lastClientId = 0;
            
            while (true)
            {
                var tcpClient = await server.AcceptTcpClientAsync();
                var client = new Client(++lastClientId, tcpClient, commandMediator);
                client.Start();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0}", e);
        }
        finally
        {
            server?.Stop();
        }

        Console.WriteLine("\nHit enter to continue...");
        Console.Read();
    }

    class InputArgs
    {
        [Option('p', "port")] public int? Port { get; set; }

        [Option('h', "host")] public string? Host { get; set; }

        [Option("dir")] public string? Directory { get; set; }

        [Option("dbfilename")] public string? DbFileName { get; set; }
    }
}