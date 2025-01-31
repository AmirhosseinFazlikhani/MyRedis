namespace Redis.Server.CommandDispatching;

public class SelectCommandFactory : ICommandFactory
{
    private const string CommandName = "SELECT";

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return new SelectCommand(args[1]);
    }
}