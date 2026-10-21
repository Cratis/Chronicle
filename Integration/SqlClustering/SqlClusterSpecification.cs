// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Orleans.Configuration;

namespace Cratis.Chronicle.Integration.SqlClustering;

public abstract class SqlClusterSpecification(bool postgres, ClusterChange change) : Specification
{
    readonly SqlCluster _cluster = new(postgres);
    readonly ClusterLoad _load = new();

    async Task Establish() => await _cluster.Start();

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

    [Fact]
    public async Task should_preserve_acknowledged_events_without_double_appends_and_observe_every_stored_event()
    {
        try
        {
            await _load.StopAndVerify();
        }
        catch
        {
            await _cluster.DumpLogs();
            throw;
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
