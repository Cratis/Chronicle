// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_an_older_attempt_starts_while_a_newer_attempt_is_active : given.a_rebuilding_constraint_index
{
    ConstraintIndexRebuildRunId _older;
    ConstraintIndexRebuildRunId _active;

    async Task Establish()
    {
        var started = DateTimeOffset.UtcNow;
        _active = new(_runId.Value, Guid.NewGuid(), started);
        _older = new(_runId.Value, Guid.NewGuid(), started.AddMinutes(-1));
        await _eventSequence.ConstraintIndexRebuildStarting(_active, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_older, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuilt(_active, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
        await Tick();
        await Tick();
    }

    [Fact] async Task should_fold_the_active_attempt_and_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_complete_the_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
