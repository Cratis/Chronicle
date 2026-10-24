// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

public class when_saving_partition_replay_completions : Specification
{
    ObserverStateStorage _storage;
    ObserverState _saved;
    ObserverState _renamed;
    ObserverState _cleared;
    ObserverState _legacy;
    readonly JobId _job = JobId.New();

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", PartitionReplayCompletions = [new("partition", _job) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, HandingOff = true, HandedOff = true }] });
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
        await _storage.Save(_renamed with { PartitionReplayCompletions = [] });
        _cleared = await _storage.Get("renamed");
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_job_identity() => _saved.PartitionReplayCompletions!.Single().JobId.ShouldEqual(_job);
    [Fact] void should_keep_the_record_after_rename() => _renamed.PartitionReplayCompletions!.Single().HandedOff.ShouldBeTrue();
    [Fact] void should_replace_the_snapshot_when_cleared() => _cleared.PartitionReplayCompletions!.ShouldBeEmpty();
    [Fact] void should_get_empty_completions_for_unstored_state() => _legacy.PartitionReplayCompletions!.ShouldBeEmpty();

    void Destroy() => _storage.Dispose();
}
