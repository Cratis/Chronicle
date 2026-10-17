// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_progress_is_unknown : given.a_rebuilding_constraint_index
{
    bool _keptBoundary;

    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
        _keptBoundary = _snapshots.Written!.ConstraintIndexCatchUpBelow is not null;
        await Tick();
        await Tick();
    }

    [Fact] void should_leave_a_durable_catch_up_boundary() => _keptBoundary.ShouldBeTrue();
    [Fact] async Task should_restore_the_cleared_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
