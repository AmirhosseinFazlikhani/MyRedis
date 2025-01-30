using Redis.Server.Persistence;
using Redis.Server.Protocol;
using Redis.Server.Replication;

namespace Redis.Server.CommandDispatching;

public class CommandFactory
{
    public static ErrorOr<ICommand> Create(string[] args, IClock clock, ClientConnection? client = null)
    {
        if (StartWith(args, "PING"))
        {
            if (args.Length > 2)
            {
                return ReplyHelper.WrongArgumentsNumberError("PING");
            }
            
            return args.Length == 1 ? new PingCommand() : new PingCommand(args[1]);
        }

        if (StartWith(args, "HELLO"))
        {
            if (args.Length > 2)
            {
                return ReplyHelper.WrongArgumentsNumberError("HELLO");
            }

            if (args.Length == 1)
            {
                return new HelloCommand(GetClient());
            }

            if (!int.TryParse(args[1], out var protocolVersion))
            {
                return ReplyHelper.IntegerParsingError();
            }
            
            return new HelloCommand(GetClient(), protocolVersion);
        }

        if (StartWith(args, "GET"))
        {
            if (args.Length != 2)
            {
                return ReplyHelper.WrongArgumentsNumberError("GET");
            }
            
            return new GetCommand(clock, args[1]);
        }

        if (StartWith(args, "SET"))
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
                return ReplyHelper.WrongArgumentsNumberError("SET");
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

                    options.Expiry = clock.Now().AddSeconds(long.Parse(optionArgs[currentOptionIndex]));
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

                    options.Expiry = clock.Now().AddMilliseconds(long.Parse(optionArgs[currentOptionIndex]));
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

            return new SetCommand(clock, options);
        }

        if (StartWith(args, ["CONFIG", "GET"]))
        {
            return new ConfigGetCommand(args[1]);
        }

        if (StartWith(args, "KEYS"))
        {
            return new KeysCommand(clock, args[1]);
        }

        if (StartWith(args, "EXPIRE"))
        {
            var options = new ExpireOptions
            {
                Key = args[1],
                ExpirySeconds = long.Parse(args[2])
            };

            if (args.Length == 4)
            {
                options.Condition = args[3];
            }

            return new ExpireCommand(clock, options);
        }

        if (StartWith(args, ["CLIENT", "GETNAME"]))
        {
            return new ClientGetNameCommand(GetClient());
        }

        if (StartWith(args, ["CLIENT", "SETNAME"]))
        {
            return new ClientSetNameCommand(GetClient(), args[2]);
        }

        if (StartWith(args, "SAVE"))
        {
            if (args.Length > 1)
            {
                return ReplyHelper.WrongArgumentsNumberError("SAVE");
            }
            
            return new SaveCommand(clock);
        }

        if (StartWith(args, "BGSAVE"))
        {
            if (args.Length > 1)
            {
                return ReplyHelper.WrongArgumentsNumberError("BGSAVE");
            }
            
            return new BGSaveCommand(clock);
        }

        if (StartWith(args, "LASTSAVE"))
        {
            if (args.Length != 1)
            {
                return ReplyHelper.WrongArgumentsNumberError("LASTSAVE");
            }

            return new LastSaveCommand();
        }

        if (StartWith(args, "REPLICAOF"))
        {
            if (args.Length != 3)
            {
                return ReplyHelper.WrongArgumentsNumberError("REPLICAOF");
            }
            
            if (!int.TryParse(args[2], out var port))
            {
                return ReplyHelper.IntegerParsingError();
            }

            var nodeAddress = new NodeAddress(args[1], port);
            return new ReplicaOfCommand(clock, nodeAddress);
        }

        if (StartWith(args, "DEL"))
        {
            if (args.Length < 2)
            {
                return ReplyHelper.WrongArgumentsNumberError("DEL");
            }
            
            return new DelCommand(args[1..]);
        }

        if (StartWith(args, "SELECT"))
        {
            if (args.Length != 2)
            {
                return ReplyHelper.WrongArgumentsNumberError("SELECT");
            }
            
            return new SelectCommand(args[1]);
        }

        return new SimpleErrorResult($"ERR unknown command '{args[0]}'");

        ClientConnection GetClient() => client ?? throw new ArgumentNullException(nameof(client));
    }

    private static bool StartWith(string[] source, string value)
    {
        return source.Length != 0 && source[0].Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartWith(string[] source, string[] value)
    {
        return source.Length >= value.Length &&
            !value.Where((t, i) => !t.Equals(source[i], StringComparison.OrdinalIgnoreCase)).Any();
    }
}