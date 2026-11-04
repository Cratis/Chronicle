// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_liveness_check_is_throttled : given.a_held_constraint_index_rebuild
{
    int _lookupsBeforeInterval;
    int _lookupsAfterRepeatedTick;

    async Task Establish()
    {
        _stepStatus = JobStepStatus.Running;
        await HoldRebuild();
    }

    async Task Because()
    {
        await Tick();
        await AppendValue("current");
        _lookupsBeforeInterval = _jobSteps.ReceivedCalls().Count();
        await PassLivenessCheck();
        await Tick();
        _lookupsAfterRepeatedTick = _jobSteps.ReceivedCalls().Count();
        await PassLivenessCheck();
    }

    [Fact] void should_not_look_up_before_the_interval() => _lookupsBeforeInterval.ShouldEqual(0);
    [Fact] void should_not_repeat_a_lookup_in_the_same_interval() => _lookupsAfterRepeatedTick.ShouldEqual(1);
    [Fact] async Task should_look_up_once_per_elapsed_interval() => await _jobSteps.Received(2).GetForJob(_jobId, Arg.Any<JobStepStatus[]>());
}
