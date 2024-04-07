﻿using DotNext;
using DotNext.Net.Cluster.Consensus.Raft;

namespace RaftNode;

internal sealed class DataModifier(IRaftCluster cluster, ISupplier<long> valueProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (!cluster.LeadershipToken.IsCancellationRequested)
            {
                var newValue = valueProvider.Invoke() + 500L;
                Console.WriteLine("Saving value {0} generated by the leader node", newValue);

                try
                {
                    var entry = new Int64LogEntry { Value = newValue, Term = cluster.Term };
                    await cluster.ReplicateAsync(entry, stoppingToken);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected error {0}", e);
                }
            }
        }
    }
}