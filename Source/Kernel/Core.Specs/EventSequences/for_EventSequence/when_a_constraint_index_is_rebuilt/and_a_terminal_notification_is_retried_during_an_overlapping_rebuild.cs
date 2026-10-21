// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_a_terminal_notification_is_retried_during_an_overlapping_rebuild : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuildStarting(_otherRunId, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        // A response lost after the first notification succeeds makes the job retry that same
        // notification. The other job has not finished and must retain its rebuilding mark.
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);
    }

    [Fact] void should_preserve_the_other_jobs_boundary() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldNotBeNull();
}
