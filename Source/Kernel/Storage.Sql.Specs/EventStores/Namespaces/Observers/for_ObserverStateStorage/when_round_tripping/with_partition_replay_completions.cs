// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;
using Microsoft.EntityFrameworkCore;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_partition_replay_completions : given.an_observer_state_storage
{
    readonly JobId _job = JobId.New();
    readonly EventType _type = new("event", 1);
    KernelObserverState _saved;
    KernelObserverState _renamed;
    KernelObserverState _legacy;
    KernelObserverState _cleared;

    async Task Because()
    {
        await _storage.Save(new()
        {
            Identifier = "observer",
            PartitionReplayCompletions =
            [
                new("partition", _job) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, ReplayedEventTypes = [_type], HandingOff = true, HandedOff = true },
                new("legacy", null) { LastHandledEventSequenceNumber = 10UL, LastScannedEventSequenceNumber = 12UL }
            ]
        });
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
        await _storage.Save(_renamed with { PartitionReplayCompletions = [] });
        _cleared = await _storage.Get("renamed");
        await _storage.Save(new() { Identifier = "legacy" });
        await using (var context = CreateContext())
            await context.Database.ExecuteSqlRawAsync("UPDATE Observers SET PartitionReplayCompletions = NULL WHERE Id = 'legacy'");
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_job_identity() => _saved.PartitionReplayCompletions!.First().JobId.ShouldEqual(_job);
    [Fact] void should_keep_the_handled_position() => _saved.PartitionReplayCompletions!.First().LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_scanned_position() => _saved.PartitionReplayCompletions!.First().LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)48UL);
    [Fact] void should_keep_replayed_types() => _saved.PartitionReplayCompletions!.First().ReplayedEventTypes.ShouldContainOnly(_type);
    [Fact] void should_keep_the_partition_after_rename() => _renamed.PartitionReplayCompletions!.First().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_handoff_phase_after_rename() => _renamed.PartitionReplayCompletions!.First().HandingOff.ShouldBeTrue();
    [Fact] void should_keep_the_concluded_job_exclusion() => _renamed.PartitionReplayCompletions!.First().HandedOff.ShouldBeTrue();
    [Fact] void should_keep_legacy_callback_identity_absent() => _renamed.PartitionReplayCompletions!.Last().JobId.ShouldBeNull();
    [Fact] void should_load_a_legacy_null_column_as_empty() => _legacy.PartitionReplayCompletions!.ShouldBeEmpty();
    [Fact] void should_persist_a_cleared_snapshot() => _cleared.PartitionReplayCompletions!.ShouldBeEmpty();
}
