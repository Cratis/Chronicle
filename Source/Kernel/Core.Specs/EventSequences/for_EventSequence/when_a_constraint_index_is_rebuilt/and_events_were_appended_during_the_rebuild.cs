// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_events_were_appended_during_the_rebuild : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await Seed(2, "source", email: "newer");
        _stateStorage.State.SequenceNumber = 3;
        await Hold("source", "newer");
        await Hold("source", "current");
    }

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], new(true, 1));

    [Fact] async Task should_restore_the_newer_claim() => (await Values("source")).ShouldContainOnly(Hash("newer"));
    [Fact] void should_clear_the_owned_boundary() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
