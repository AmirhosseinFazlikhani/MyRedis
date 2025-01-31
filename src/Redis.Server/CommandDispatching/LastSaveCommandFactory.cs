using Redis.Server.Persistence;

namespace Redis.Server.CommandDispatching;

public class LastSaveCommandFactory : ICommandFactory
{
    private const string CommandName = "LASTSAVE";

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length > 1)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return new LastSaveCommand();
    }
}