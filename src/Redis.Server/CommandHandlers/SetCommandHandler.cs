using RESP.DataTypes;

namespace Redis.Server.CommandHandlers;

public class SetCommandHandler : ICommandHandler
{
    private readonly IClock _clock;

    public SetCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public IRespData Handle(string[] parameters, RequestContext context)
    {
        var entry = new Entry(parameters[2]);

        var options = parameters.AsSpan(3..);
        var optionsCount = options.Length;

        if (optionsCount > 4)
        {
            return ReplyHelper.WrongArgumentsNumberError("SET");
        }

        var setCond = SetCond.None;
        var keepTtl = false;

        var currentOptionIndex = 0;
        while (currentOptionIndex < options.Length)
        {
            if (options[currentOptionIndex].Equals("ex", StringComparison.OrdinalIgnoreCase))
            {
                currentOptionIndex++;

                if (!long.TryParse(options[currentOptionIndex], out var seconds))
                {
                    return ReplyHelper.IntegerParsingError();
                }

                if (entry.Expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                entry.Expiry = _clock.Now().AddSeconds(seconds);
            }
            else if (options[currentOptionIndex].Equals("px", StringComparison.OrdinalIgnoreCase))
            {
                currentOptionIndex++;

                if (!long.TryParse(options[currentOptionIndex], out var milliseconds))
                {
                    return ReplyHelper.IntegerParsingError();
                }

                if (entry.Expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                entry.Expiry = _clock.Now().AddMilliseconds(milliseconds);
            }
            else if (options[currentOptionIndex].Equals("xx", StringComparison.OrdinalIgnoreCase))
            {
                if (setCond != SetCond.None)
                {
                    return ReplyHelper.SyntaxError();
                }

                setCond = SetCond.Exists;
            }
            else if (options[currentOptionIndex].Equals("nx", StringComparison.OrdinalIgnoreCase))
            {
                if (setCond != SetCond.None)
                {
                    return ReplyHelper.SyntaxError();
                }

                setCond = SetCond.NotExists;
            }
            else if (options[currentOptionIndex].Equals("keepttl", StringComparison.OrdinalIgnoreCase))
            {
                keepTtl = true;
            }
            else
            {
                return ReplyHelper.SyntaxError();
            }

            currentOptionIndex++;
        }

        switch (setCond)
        {
            case SetCond.None:
                SetValue(parameters[1], entry, keepTtl);
                break;
            case SetCond.Exists:
                lock (parameters[1])
                {
                    if (!DatabaseProvider.Database.TryGetValue(parameters[1], out var value) || value.IsExpired(_clock))
                    {
                        return new RespBulkString(null);
                    }

                    SetValue(parameters[1], entry, keepTtl);
                }

                break;
            case SetCond.NotExists:
                lock (parameters[1])
                {
                    if (DatabaseProvider.Database.TryGetValue(parameters[1], out var value) && !value.IsExpired(_clock))
                    {
                        return new RespBulkString(null);
                    }

                    SetValue(parameters[1], entry, keepTtl);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ReplyHelper.OK();
    }

    private void SetValue(string key, Entry entry, bool keepTtl)
    {
        if (keepTtl)
        {
            DatabaseProvider.Database.AddOrUpdate(key, entry, (_, oldEntry) =>
            {
                if (!oldEntry.IsExpired(_clock))
                {
                    entry.Expiry = oldEntry.Expiry;
                }

                return entry;
            });
        }
        else
        {
            DatabaseProvider.Database[key] = entry;
        }
    }
}