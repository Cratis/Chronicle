// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_starting_is_delivered_twice : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
    }

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);

    [Fact] void should_release_the_single_run() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
