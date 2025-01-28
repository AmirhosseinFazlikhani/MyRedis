using Redis.Server.Protocol;

namespace Redis.Server;

public class ConfigGetCommand : ICommand
{
    private readonly string _configName;

    public ConfigGetCommand(string configName)
    {
        _configName = configName;
    }

    public IResult Execute()
    {
        if (_configName.Equals("dir", StringComparison.OrdinalIgnoreCase))
        {
            return new ArrayResult([
                new BulkStringResult("dir"),
                new BulkStringResult(Configuration.Directory)
            ]);
        }

        if (_configName.Equals("dbfilename", StringComparison.OrdinalIgnoreCase))
        {
            return new ArrayResult([
                new BulkStringResult("dbfilename"),
                new BulkStringResult(Configuration.DbFileName)
            ]);
        }

        return new SimpleErrorResult(
            "(error) ERR Unknown subcommand or wrong number of arguments for 'get'. Try CONFIG HELP.");
    }
}