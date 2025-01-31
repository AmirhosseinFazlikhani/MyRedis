namespace Redis.Server.CommandDispatching;

public class DelCommandFactory : ICommandFactory
{
    private const string CommandName = "DEL";

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length < 2)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return new DelCommand(args[1..]);
    }
}