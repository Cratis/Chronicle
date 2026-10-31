// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_a_late_starting_arrives_for_an_attempt_already_seen_in_a_rebuilt : given.a_rebuilding_constraint_index
{
    ConstraintIndexRebuildRunId _old;
    ConstraintIndexRebuildRunId _current;

    async Task Establish()
    {
        _old = new(_runId.Value, Guid.NewGuid());
        _current = new(_runId.Value, Guid.NewGuid());
        await _eventSequence.ConstraintIndexRebuildStarting(_current, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
        await _eventSequence.ConstraintIndexRebuilt(_old, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_old, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuilt(_current, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
        await Tick();
        await Tick();
    }

    [Fact] async Task should_restore_the_history_when_the_current_attempt_ends() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_complete_the_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
