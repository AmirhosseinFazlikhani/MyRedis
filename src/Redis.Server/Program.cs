using System.Net;
using System.Net.Sockets;
using CommandLine;
using Redis.Server.Persistence;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

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

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .WriteTo.Debug()
            .WriteTo.Console()
            .CreateLogger();

        TcpListener? server = null;

        try
        {
            var clock = new Clock();
            RdbFile.Load(clock);

            server = new TcpListener(IPAddress.Parse(Configuration.Host), Configuration.Port);
            server.Start();

            Log.Information("Server is now listening on {Host}:{Port}",
                Configuration.Host,
                Configuration.Port);

            using var commandMediator = new CommandConsumer(clock);
            await ClientManager.AcceptClientAsync(server, commandMediator);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Application shutting down");
        }
        finally
        {
            server?.Stop();
        }
    }

    class InputArgs
    {
        [Option('p', "port")] public int? Port { get; set; }

        [Option('h', "host")] public string? Host { get; set; }

        [Option("dir")] public string? Directory { get; set; }

        [Option("dbfilename")] public string? DbFileName { get; set; }
    }
}