// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_the_rebuild_failed_after_clearing : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], new(true, 0));

    [Fact] async Task should_restore_the_cleared_claim() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
