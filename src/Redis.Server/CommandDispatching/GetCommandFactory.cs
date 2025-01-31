namespace Redis.Server.CommandDispatching;

public class GetCommandFactory : ICommandFactory
{
    private const string CommandName = "GET";

    private readonly IClock _clock;

    public GetCommandFactory(IClock clock)
    {
        _clock = clock;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length != 2)
        {
            return ReplyHelper.WrongArgumentsNumberError("GET");
        }

        return new GetCommand(_clock, args[1]);
    }
}