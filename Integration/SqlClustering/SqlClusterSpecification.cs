// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Orleans.Configuration;

namespace Cratis.Chronicle.Integration.SqlClustering;

public abstract class SqlClusterSpecification(bool postgres, ClusterChange change, string? initialImage = null) : Specification
{
    readonly SqlCluster _cluster = new(postgres, initialImage);
    readonly ClusterLoad _load = new(TransitionsFor(change));

    async Task Establish() => await _cluster.Start();

    // A rolling upgrade replaces every node of the three-node cluster, and each replacement is a transition of its own.
    static int TransitionsFor(ClusterChange change) => change == ClusterChange.RollingUpgrade ? 3 : 1;

    async Task Because()
    {
        try
        {
            await _load.Start(_cluster);
            await _load.WaitForProgress(10);
            switch (change)
            {
                case ClusterChange.Join:
                    await _cluster.WaitForMembership(entries => entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3, TimeSpan.FromSeconds(30));
                    break;
                case ClusterChange.Kill:
                    await KillOne();
                    break;
                case ClusterChange.Restart:
                    await RestartAll();
                    break;
                case ClusterChange.RollingUpgrade:
                    await UpgradeOneByOne();
                    break;
            }

            await _load.WaitForProgress(_load.Successes + 20);
        }
        catch
        {
            await _cluster.DumpLogs();
            throw;
        }
    }

    async Task Destroy()
    {
        try
        {
            await _load.DisposeAsync();
        }
        finally
        {
            await _cluster.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that acknowledged events were neither lost nor double appended and that every stored event was observed.
    /// Each provider scenario declares the fact itself, so a scenario with a known product gap can be skipped with a reason.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task VerifyAcknowledgedEventsAreObserved()
    {
        try
        {
            await _load.StopAndVerify();
        }
        finally
        {
            await _cluster.DumpLogs();
        }
    }

    async Task KillOne()
    {
        var before = await _cluster.WaitForMembership(entries => entries.Count == 3, TimeSpan.FromSeconds(10));
        _load.SiloDeparting(before.Single(entry => entry.Address == _cluster.Nodes[0].IpAddress && entry.Status == (int)SiloStatus.Active));

        // SIGKILL bypasses Orleans' graceful leave path. Target only the container owned by this fixture.
        using var process = Process.Start(new ProcessStartInfo("docker")
        {
            ArgumentList = { "kill", "--signal=KILL", _cluster.Nodes[0].Id },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        process.ExitCode.ShouldEqual(0);
        var membership = new ClusterMembershipOptions();

        // Keep product defaults: missed probes alone omit voting and heartbeat propagation.
        // Allow their default windows plus 15 seconds of scheduling margin (3 minutes total).
        var failureWindow = (membership.ProbeTimeout * membership.NumMissedProbesLimit)
            + membership.DeathVoteExpirationTimeout
            + membership.IAmAliveTablePublishTimeout
            + TimeSpan.FromSeconds(15);
        var clock = Stopwatch.StartNew();
        var after = await _cluster.WaitForMembership(
            entries => entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 2 && entries.Count(entry => entry.Status == (int)SiloStatus.Dead) == 1,
            failureWindow);
        Console.WriteLine($"Failure detection: {clock.Elapsed}. Membership rows before={before.Count}, after={after.Count}");
    }

    async Task UpgradeOneByOne()
    {
        var previous = await _cluster.WaitForMembership(entries => entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3, TimeSpan.FromSeconds(10));
        for (var index = 0; index < _cluster.Nodes.Count; index++)
        {
            var active = previous.Single(entry => entry.Address == _cluster.Nodes[index].IpAddress && entry.Status == (int)SiloStatus.Active);
            _load.SiloDeparting(active);
            await _cluster.Replace(index, SqlCluster.CurrentImage);

            // Until the last node is replaced the cluster runs both releases, and the load keeps every grain call
            // crossing between them - the state a rolling deployment leaves a cluster in.
            previous = await _cluster.WaitForMembership(
                entries =>
                    entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3 &&
                    entries.Any(entry => entry.Address == active.Address && entry.Generation == active.Generation && entry.Status == (int)SiloStatus.Dead),
                TimeSpan.FromSeconds(90));
            await _load.WaitForProgress(_load.Successes + 10);
        }
    }

    async Task RestartAll()
    {
        var previous = await _cluster.WaitForMembership(entries => entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3, TimeSpan.FromSeconds(10));
        foreach (var node in _cluster.Nodes)
        {
            var active = previous.Single(entry => entry.Address == node.IpAddress && entry.Status == (int)SiloStatus.Active);

            // Docker may hand a restarted container another address, so only the old incarnation is identified exactly.
            _load.SiloDeparting(active);
            await node.StopAsync().WaitAsync(TimeSpan.FromSeconds(90));
            await node.StartAsync().WaitAsync(TimeSpan.FromSeconds(90));
            previous = await _cluster.WaitForMembership(entries =>
                entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3 &&
                entries.Any(entry => entry.Address == active.Address && entry.Generation == active.Generation && entry.Status == (int)SiloStatus.Dead),
                TimeSpan.FromSeconds(90));
            await _load.WaitForProgress(_load.Successes + 10);
        }
    }
}
