// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_reporting_step_is_still_running : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        _stepStatus = JobStepStatus.Running;
        await HoldRebuild();
    }

    async Task Because()
    {
        await PassLivenessCheck();
        await PassLivenessCheck();
        await PassLivenessCheck();
        _result = await AppendValue("current");
    }

    [Fact] void should_refuse_governed_appends() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_not_write_catch_up_claims() => (await Values("source")).ShouldBeEmpty();
}
