namespace Redis.Server.CommandDispatching;

public class SetCommandFactory : ICommandFactory
{
    private const string CommandName = "SET";

    private readonly IClock _clock;

    public SetCommandFactory(IClock clock)
    {
        _clock = clock;
    }

    public bool Matches(string[] args) => args.StartWith(CommandName);

    public ErrorOr<ICommand> Create(string[] args, IScope scope)
    {
        var options = new SetOptions
        {
            Key = args[1],
            Value = args[2],
            KeepTtl = false,
            Condition = SetCond.None,
        };

        var optionArgs = args.AsSpan(3..);
        var optionArgsCount = optionArgs.Length;

        if (optionArgsCount > 4)
        {
            return ReplyHelper.WrongArgumentsNumberError(CommandName);
        }

        var currentOptionIndex = 0;
        while (currentOptionIndex < optionArgs.Length)
        {
            if (optionArgs[currentOptionIndex].Equals("EX", StringComparison.OrdinalIgnoreCase))
            {
                currentOptionIndex++;

                if (!long.TryParse(optionArgs[currentOptionIndex], out var seconds))
                {
                    return ReplyHelper.IntegerParsingError();
                }

                if (options.Expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                options.Expiry = _clock.Now().AddSeconds(seconds);
            }
            else if (optionArgs[currentOptionIndex].Equals("PX", StringComparison.OrdinalIgnoreCase))
            {
                currentOptionIndex++;

                if (!long.TryParse(optionArgs[currentOptionIndex], out var milliseconds))
                {
                    return ReplyHelper.IntegerParsingError();
                }

                if (options.Expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                options.Expiry = _clock.Now().AddMilliseconds(milliseconds);
            }
            else if (optionArgs[currentOptionIndex].Equals("XX", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Condition != SetCond.None)
                {
                    return ReplyHelper.SyntaxError();
                }

                options.Condition = SetCond.Exists;
            }
            else if (optionArgs[currentOptionIndex].Equals("NX", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Condition != SetCond.None)
                {
                    return ReplyHelper.SyntaxError();
                }

                options.Condition = SetCond.NotExists;
            }
            else if (optionArgs[currentOptionIndex].Equals("KEEPTTL", StringComparison.OrdinalIgnoreCase))
            {
                options.KeepTtl = true;
            }
            else
            {
                return ReplyHelper.SyntaxError();
            }

            currentOptionIndex++;
        }

        return new SetCommand(_clock, options);
    }
}