// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The partitions a newer observer-wide catch-up registered have no partition jobs of their own, so a stale job
/// reporting back would find them releasable; they belong to the newer job and stay held.
/// </summary>
public class and_a_stale_job_reports_after_the_newer_catch_up_registered_its_partitions : given.a_newer_catch_up_started_after_a_stale_one
{
    readonly Key _partition = "partition";

    async Task Establish()
    {
        await StartNewerCatchUp();
        await _observer.RegisterCatchingUpPartitions([_partition]);
    }

    Task Because() => ReportStaleCatchUp();

    [Fact] void should_keep_the_partition_held() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim.ShouldEqual(_newerClaim);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(_writesBefore);
}
