using Redis.Server.Persistence;

namespace Redis.Server.CommandDispatching;

public class BGSaveCommandFactory : ICommandFactory
{
    private const string CommandName = "BGSAVE";

    private readonly IClock _clock;

    public BGSaveCommandFactory(IClock clock)
    {
        _clock = clock;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length > 1)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        return new BGSaveCommand(_clock);
    }
}