namespace Redis.Server.CommandDispatching;

public class KeysCommandFactory : ICommandFactory
{
    private const string CommandName = "KEYS";

    private readonly IClock _clock;

    public KeysCommandFactory(IClock clock)
    {
        _clock = clock;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return new KeysCommand(_clock, args[1]);
    }
}