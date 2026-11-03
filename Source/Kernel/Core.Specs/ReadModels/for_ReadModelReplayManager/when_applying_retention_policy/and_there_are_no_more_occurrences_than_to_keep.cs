// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManager.when_applying_retention_policy;

public class and_there_are_no_more_occurrences_than_to_keep : given.a_read_model_replay_manager
{
    protected override IEnumerable<ReadModelOccurrence> OccurrencesToSeed => [OccurrenceStartedAt(0)];

    Task Because() => _manager.ApplyRetentionPolicy(1);

    [Fact] async Task should_keep_the_occurrence() => (await _manager.GetOccurrences()).ShouldContainOnly(_seeded);
    [Fact] void should_not_remove_any_occurrence() => _stateStorage.State.RemovedOccurrences.ShouldBeEmpty();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
