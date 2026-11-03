// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManager.when_applying_retention_policy;

public class and_versions_to_keep_is_negative : given.a_read_model_replay_manager
{
    protected override IEnumerable<ReadModelOccurrence> OccurrencesToSeed => [OccurrenceStartedAt(0), OccurrenceStartedAt(1)];

    Task Because() => _manager.ApplyRetentionPolicy(-2);

    [Fact] async Task should_treat_it_as_zero_and_keep_no_occurrences() => (await _manager.GetOccurrences()).ShouldBeEmpty();
    [Fact] void should_mark_all_occurrences_as_removed() => _stateStorage.State.RemovedOccurrences.ShouldContainOnly(_seeded);
}
