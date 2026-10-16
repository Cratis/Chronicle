// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_the_job_is_lost : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);
    }

    async Task Because()
    {
        await Reactivate();
        await Tick();
    }

    [Fact] async Task should_recover_the_cleared_history_on_activation() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
