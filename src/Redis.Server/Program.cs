using System.Net;
using System.Net.Sockets;
using CommandLine;

namespace Redis.Server;

class Program
{
    class Options
    {
        [Option('p', "port", Required = false, Default = 6379)]
        public int Port { get; set; }
        
        [Option('h', "Host", Required = false, Default = "127.0.0.1")]
        public string Host { get; set; }
    }
    
    static async Task Main(string[] args)
    {
        var options = Parser.Default.ParseArguments<Options>(args);

        if (options.Errors.Any())
        {
            return;
        }

        TcpListener? server = null;

        try
        {
            server = new TcpListener(IPAddress.Parse(options.Value.Host), options.Value.Port);
            server.Start();
            Console.WriteLine("Server is now listening on {0}:{1}", options.Value.Host, options.Value.Port);

            var lastConnectionId = 0;

            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("New connection!");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                new Connection(++lastConnectionId, client).StartAsync();
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
}