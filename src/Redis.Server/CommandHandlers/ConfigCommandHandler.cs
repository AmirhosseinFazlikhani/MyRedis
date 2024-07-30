using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class ConfigCommandHandler : ICommandHandler
{
    private readonly Configuration _configuration;

    public ConfigCommandHandler(Configuration configuration)
    {
        _configuration = configuration;
    }

    public IRespData Handle(string[] parameters, RequestContext context)
    {
        const string subcommandOrArgumentError =
            "(error) ERR Unknown subcommand or wrong number of arguments for 'get'. Try CONFIG HELP.";

        if (parameters.Length != 3)
        {
            return new RespSimpleError(subcommandOrArgumentError);
        }

        if (parameters[1].Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            if (parameters[2].Equals("dir", StringComparison.OrdinalIgnoreCase))
            {
                return new RespArray([
                    new RespBulkString("dir"),
                    new RespBulkString(_configuration.Directory)
                ]);
            } 
            
            if (parameters[2].Equals("dbfilename", StringComparison.OrdinalIgnoreCase))
            {
                return new RespArray([
                    new RespBulkString("dbfilename"),
                    new RespBulkString(_configuration.DbFileName)
                ]);
            }
        }

        return new RespSimpleError(subcommandOrArgumentError);
    }
}