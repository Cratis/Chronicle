// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Integration.SqlClustering.PostgreSql;

public class when_stopping_a_node_gracefully : Specification
{
    readonly SqlCluster _cluster = new(true);
    MembershipEntry _stoppedSilo = null!;
    MembershipEntry[] _remainingSilos = [];
    IReadOnlyList<MembershipEntry> _membership = [];
    long _exitCode;
    TimeSpan _shutdownDuration;

    async Task Establish()
    {
        await _cluster.Start();
        var before = await _cluster.WaitForMembership(
            entries => entries.Count == 3 && entries.All(entry => entry.Status == (int)SiloStatus.Active),
            TimeSpan.FromMinutes(1));
        var stopping = before.Single(entry => entry.Address == _cluster.Nodes[0].IpAddress);
        _stoppedSilo = stopping with { Status = (int)SiloStatus.Dead };
        _remainingSilos = before.Where(entry => entry != stopping).ToArray();
    }

    async Task Because()
    {
        var node = _cluster.Nodes[0];
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(105));
        var clock = Stopwatch.StartNew();

        // Docker stop sends TERM, then KILL only after the fixture's configured 90-second stop timeout.
        await node.StopAsync(deadline.Token);
        _shutdownDuration = clock.Elapsed;
        _exitCode = await node.GetExitCodeAsync();
        _membership = await _cluster.WaitForMembership(
            entries => entries.Contains(_stoppedSilo) && _remainingSilos.All(entries.Contains),
            TimeSpan.FromMinutes(3));
    }

    async Task Destroy() => await _cluster.DisposeAsync();

    [Fact]
    public void should_exit_successfully_instead_of_being_terminated_by_a_signal() => _exitCode.ShouldEqual(0L);
    [Fact]
    public void should_stop_before_the_container_kill_timeout() => _shutdownDuration.ShouldBeLessThan(TimeSpan.FromSeconds(90));
    [Fact]
    public void should_mark_the_stopped_silo_address_and_generation_dead() => _membership.ShouldContain(_stoppedSilo);
    [Fact]
    public void should_keep_the_other_silos_active() => _membership.Where(entry => entry.Status == (int)SiloStatus.Active).ShouldContainOnly(_remainingSilos);
}
