// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_the_rebuild_processed_nothing : given.a_rebuilding_constraint_index
{
    async Task Establish() => await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);

    [Fact] void should_not_fold() => _rangeReads.ShouldEqual(0);
    [Fact] void should_clear_the_boundary() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
