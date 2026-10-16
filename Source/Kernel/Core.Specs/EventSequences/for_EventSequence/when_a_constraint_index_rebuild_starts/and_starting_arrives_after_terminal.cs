// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_starting_arrives_after_terminal : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);
    }

    async Task Because() => await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);

    [Fact] void should_not_recreate_the_completed_run() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
