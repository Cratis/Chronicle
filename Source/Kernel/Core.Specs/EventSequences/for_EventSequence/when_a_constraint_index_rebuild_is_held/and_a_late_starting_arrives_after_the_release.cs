// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_a_late_starting_arrives_after_the_release : given.a_held_constraint_index_rebuild
{
    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        await _eventSequence.ConstraintIndexRebuildStarting(_heldRunId, [_uniqueDefinition.Name]);
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] void should_not_mark_a_finished_run_again() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
