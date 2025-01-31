namespace Redis.Server.CommandDispatching;

public class ExpireCommandFactory : ICommandFactory
{
    private const string CommandName = "EXPIRE";

    private readonly IClock _clock;

    public ExpireCommandFactory(IClock clock)
    {
        _clock = clock;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        if (args.Length is < 3 or > 4)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        if (!long.TryParse(args[2], out var expirySeconds))
        {
            return ReplyHelper.IntegerParsingError();
        }

        var options = new ExpireOptions
        {
            Key = args[1],
            ExpirySeconds = expirySeconds
        };

        if (args.Length == 4)
        {
            options.Condition = args[3];
        }

        return new ExpireCommand(_clock, options);
    }
}