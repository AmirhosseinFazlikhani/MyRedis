using RESP.DataTypes;

namespace Redis.Server;

public class ConfigCommandHandler
{
    public static IRespData Handle(string[] args)
    {
        const string subcommandOrArgumentError =
            "(error) ERR Unknown subcommand or wrong number of arguments for 'get'. Try CONFIG HELP.";

        if (args.Length != 3)
        {
            return new RespSimpleError(subcommandOrArgumentError);
        }

        if (args[1].Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            if (args[2].Equals("dir", StringComparison.OrdinalIgnoreCase))
            {
                return new RespArray([
                    new RespBulkString("dir"),
                    new RespBulkString(Configuration.Directory)
                ]);
            }

            if (args[2].Equals("dbfilename", StringComparison.OrdinalIgnoreCase))
            {
                return new RespArray([
                    new RespBulkString("dbfilename"),
                    new RespBulkString(Configuration.DbFileName)
                ]);
            }
        }

        return new RespSimpleError(subcommandOrArgumentError);
    }
}