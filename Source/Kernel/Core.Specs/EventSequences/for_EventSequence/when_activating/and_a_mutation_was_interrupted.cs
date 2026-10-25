// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating;

public class and_a_mutation_was_interrupted : given.an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceMutationEpoch InitialMutationEpoch => 5UL;

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_the_settled_epoch_on_activation() => _snapshots.Snapshots[0].MutationEpoch.Value.ShouldEqual(6UL);
    [Fact] void should_not_leave_the_epoch_mutating() => _stateStorage.State.MutationEpoch.IsMutating.ShouldBeFalse();
}
