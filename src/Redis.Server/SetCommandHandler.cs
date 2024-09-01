using RESP.DataTypes;

namespace Redis.Server;

public class SetCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock)
    {
        var key = args[1];
        var value = args[2];
        DateTime? expiry = null;

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

                if (expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                expiry = clock.Now().AddSeconds(seconds);
            }
            else if (options[currentOptionIndex].Equals("px", StringComparison.OrdinalIgnoreCase))
            {
                currentOptionIndex++;

                if (!long.TryParse(options[currentOptionIndex], out var milliseconds))
                {
                    return ReplyHelper.IntegerParsingError();
                }

                if (expiry.HasValue)
                {
                    return ReplyHelper.SyntaxError();
                }

                expiry = clock.Now().AddMilliseconds(milliseconds);
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
                SetValue();
                break;
            case SetCond.Exists:
                if (!DataStore.ContainsKey(key, clock))
                {
                    return new RespBulkString(null);
                }

                SetValue();
                break;
            case SetCond.NotExists:
                if (DataStore.ContainsKey(key, clock))
                {
                    return new RespBulkString(null);
                }

                SetValue();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ReplyHelper.OK();

        void SetValue()
        {
            DataStore.KeyValueStore[key] = value;

            if (!keepTtl)
            {
                if (expiry.HasValue)
                {
                    DataStore.KeyExpiryStore[key] = expiry.Value;
                }
                else
                {
                    DataStore.KeyExpiryStore.Remove(key);
                }
            }
            else if(DataStore.KeyExpiryStore.TryGetValue(key, out var oldExpiry) && oldExpiry < clock.Now())
            {
                DataStore.KeyExpiryStore.Remove(key);
            }
        }
    }
}