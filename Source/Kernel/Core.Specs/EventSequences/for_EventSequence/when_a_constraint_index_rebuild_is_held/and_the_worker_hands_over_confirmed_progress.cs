// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_worker_hands_over_confirmed_progress : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        await HoldRebuild();
        await Hold("source", "current");
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuilt(_heldRunId, [_uniqueDefinition.Name], new(true, 1));
        _result = await AppendValue("new-value");
    }

    [Fact] void should_not_add_a_catch_up_refusal() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_admit_governed_appends_without_waiting_for_the_timer() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_preserve_the_workers_claims() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
