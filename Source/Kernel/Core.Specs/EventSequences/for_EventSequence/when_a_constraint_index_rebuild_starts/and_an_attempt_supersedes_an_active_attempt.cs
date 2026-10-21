// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_an_attempt_supersedes_an_active_attempt : given.a_rebuilding_constraint_index
{
    ConstraintIndexRebuildRunId _retry;

    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(new(_runId.Value, Guid.NewGuid()), [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
        _retry = new(_runId.Value, Guid.NewGuid());
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_retry, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuilt(_retry, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);
        await Tick();
        await Tick();
    }

    [Fact] async Task should_release_the_superseded_guard_and_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_complete_the_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_advance_the_watermark() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
