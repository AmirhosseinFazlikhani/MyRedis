namespace Redis.Server.CommandDispatching;

public class ConfigGetCommandFactory : ICommandFactory
{
    public bool Matches(string[] args) => args.StartWith(["CONFIG", "GET"]);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 3)
        {
            return ReplyHelper.WrongArgumentsNumberError("CONFIG GET");
        }

        return new ConfigGetCommand(args[2]);
    }
}