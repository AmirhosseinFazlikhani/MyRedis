using RESP.DataTypes;

namespace Redis.Server.Replication;

public static class ReplicaOfCommandHandler
{
    public static IRespData Handle(string[] args, IClock clock, ICommandHandler commandConsumer)
    {
        if (args.Length != 3)
        {
            return ReplyHelper.WrongArgumentsNumberError("REPLICAOF");
        }

        if (args[1].Equals("NO", StringComparison.OrdinalIgnoreCase) &&
            args[2].Equals("ONE", StringComparison.OrdinalIgnoreCase))
        {
            ReplicationManager.ReplicaOfNoOne();
        }

        if (!int.TryParse(args[2], out var port))
        {
            return new RespSimpleError("ERR Invalid master port");
        }

        ReplicationManager.ReplicaOf(new NodeAddress(args[1], port), clock, commandConsumer);
        return ReplyHelper.OK();
    }
}