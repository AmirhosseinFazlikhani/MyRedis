using Redis.Server.Protocol;

namespace Redis.Server.Replication;

public class ReplicaOfCommandHandler : ICommand
{
    private readonly IClock _clock;
    private readonly NodeAddress? _nodeAddress;

    public ReplicaOfCommandHandler(IClock clock, NodeAddress? nodeAddress)
    {
        _clock = clock;
        _nodeAddress = nodeAddress;
    }

    public IResult Execute()
    {
        if (_nodeAddress.HasValue)
        {
            ReplicationManager.ReplicaOf(_nodeAddress.Value, _clock);
        }
        else
        {
            ReplicationManager.ReplicaOfNoOne();
        }

        return ReplyHelper.OK();
    }
}