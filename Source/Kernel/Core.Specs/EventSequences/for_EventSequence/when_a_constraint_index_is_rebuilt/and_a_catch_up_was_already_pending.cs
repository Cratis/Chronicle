// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_a_catch_up_was_already_pending : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        _stateStorage.State.ConstraintIndexCatchUpBelow = 1;
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
    }

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], new(true, 1));

    [Fact] void should_preserve_the_existing_boundary() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldEqual(new EventSequenceNumber(1));
}
