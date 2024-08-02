using RESP.DataTypes;

namespace Redis.Server;

public class SetCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock)
    {
        var entry = new Entry(args[2]);

        var options = args.AsSpan(3..);
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

                entry.Expiry = clock.Now().AddSeconds(seconds);
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

                entry.Expiry = clock.Now().AddMilliseconds(milliseconds);
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
                SetValue(args[1], entry, keepTtl, clock);
                break;
            case SetCond.Exists:
                lock (args[1])
                {
                    if (!DatabaseProvider.Database.TryGetValue(args[1], out var value) || value.IsExpired(clock))
                    {
                        return new RespBulkString(null);
                    }

                    SetValue(args[1], entry, keepTtl, clock);
                }

                break;
            case SetCond.NotExists:
                lock (args[1])
                {
                    if (DatabaseProvider.Database.TryGetValue(args[1], out var value) && !value.IsExpired(clock))
                    {
                        return new RespBulkString(null);
                    }

                    SetValue(args[1], entry, keepTtl, clock);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ReplyHelper.OK();
    }

    private static void SetValue(string key, Entry entry, bool keepTtl, IClock clock)
    {
        if (keepTtl)
        {
            DatabaseProvider.Database.AddOrUpdate(key, entry, (_, oldEntry) =>
            {
                if (!oldEntry.IsExpired(clock))
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