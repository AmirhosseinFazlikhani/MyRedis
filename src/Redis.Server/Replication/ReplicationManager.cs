﻿using Redis.Server.CommandDispatching;

namespace Redis.Server.Replication;

public static class ReplicationManager
{
    private static Replica? _replica;

    public static NodeRole Role { get; private set; }

    public static void ReplicaOf(NodeAddress masterAddress, IClock clock, CommandFactory commandFactory)
    {
        if (_replica?.Status is ReplicaStatus.Initializing or ReplicaStatus.Running)
        {
            _replica.CancelAsync().Wait();
        }

        Role = NodeRole.Replica;
        _replica = new Replica(masterAddress, clock, commandFactory);
    }

    public static void ReplicaOfNoOne()
    {
        if (_replica?.Status is ReplicaStatus.Initializing or ReplicaStatus.Running)
        {
            _replica.CancelAsync().Wait();
        }

        Role = NodeRole.Master;
    }
}