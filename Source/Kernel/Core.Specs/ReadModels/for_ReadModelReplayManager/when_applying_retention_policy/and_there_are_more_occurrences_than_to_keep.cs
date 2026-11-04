// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManager.when_applying_retention_policy;

public class and_there_are_more_occurrences_than_to_keep : given.a_read_model_replay_manager
{
    protected override IEnumerable<ReadModelOccurrence> OccurrencesToSeed => [OccurrenceStartedAt(1), OccurrenceStartedAt(2), OccurrenceStartedAt(0)];

    Task Because() => _manager.ApplyRetentionPolicy(1);

    [Fact] async Task should_keep_only_the_newest_occurrence() => (await _manager.GetOccurrences()).ShouldContainOnly(OccurrenceStartedAt(2));
    [Fact] void should_mark_the_other_occurrences_as_removed() => _stateStorage.State.RemovedOccurrences.ShouldContainOnly(OccurrenceStartedAt(0), OccurrenceStartedAt(1));
    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
}
