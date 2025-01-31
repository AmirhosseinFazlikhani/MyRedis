namespace Redis.Server.CommandDispatching;

public class PingCommandFactory : ICommandFactory
{
    private const string CommandName = "PING";

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length > 2)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return args.Length == 1 ? new PingCommand() : new PingCommand(args[1]);
    }
}