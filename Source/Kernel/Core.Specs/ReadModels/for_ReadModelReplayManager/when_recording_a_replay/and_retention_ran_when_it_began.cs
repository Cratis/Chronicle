// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManager.when_recording_a_replay;

public class and_retention_ran_when_it_began : given.a_read_model_replay_manager
{
    protected override IEnumerable<ReadModelOccurrence> OccurrencesToSeed => [OccurrenceStartedAt(0)];

    async Task Because()
    {
        await _manager.ApplyRetentionPolicy(1);
        await _manager.Replayed("some-observer", ReplayStartedAt(1));
    }

    [Fact] async Task should_hold_the_previous_and_the_new_occurrence() => (await _manager.GetOccurrences()).Count().ShouldEqual(2);
    [Fact] async Task should_keep_the_previous_occurrence() => (await _manager.GetOccurrences()).ShouldContain(OccurrenceStartedAt(0));
    [Fact] async Task should_record_the_new_occurrence() => (await _manager.GetOccurrences()).ShouldContain(OccurrenceStartedAt(1));
    [Fact] void should_not_remove_the_previous_occurrence() => _stateStorage.State.RemovedOccurrences.ShouldBeEmpty();
}
