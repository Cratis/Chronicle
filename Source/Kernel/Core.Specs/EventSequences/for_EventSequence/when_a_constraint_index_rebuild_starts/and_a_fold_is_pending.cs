// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_a_fold_is_pending : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        _stateStorage.State.ConstraintIndexWatermark = 0;
        _stateStorage.State.ConstraintIndexCatchUpBelow = 2;
        await _log.SaveState(_stateStorage.State);
        await Reactivate();
        await _claims.ClearValues(_uniqueDefinition);
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await Tick();
        await Tick();
    }

    [Fact] void should_not_write_the_rebuilding_constraint() => _claimWrites.ShouldEqual(0);
    [Fact] async Task should_leave_the_rebuilding_scope_alone() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_keep_the_boundary() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldNotBeNull();
}
