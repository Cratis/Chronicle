// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_check_interval_has_not_elapsed : given.a_held_constraint_index_rebuild
{
    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await Tick();
        _result = await AppendValue("current");
    }

    [Fact] void should_not_query_step_storage() => _jobSteps.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_the_rebuild_refusal() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
}
