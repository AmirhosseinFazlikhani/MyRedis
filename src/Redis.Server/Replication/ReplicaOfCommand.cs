using Redis.Server.CommandDispatching;
using Redis.Server.Protocol;

namespace Redis.Server.Replication;

public class ReplicaOfCommand : ICommand
{
    private readonly IClock _clock;
    private readonly NodeAddress? _nodeAddress;
    private readonly CommandFactory _commandFactory;

    public ReplicaOfCommand(IClock clock, NodeAddress? nodeAddress, CommandFactory commandFactory)
    {
        _clock = clock;
        _nodeAddress = nodeAddress;
        _commandFactory = commandFactory;
    }

    public IResult Execute()
    {
        if (_nodeAddress.HasValue)
        {
            ReplicationManager.ReplicaOf(_nodeAddress.Value, _clock, _commandFactory);
        }
        else
        {
            ReplicationManager.ReplicaOfNoOne();
        }

        return ReplyHelper.OK();
    }
}