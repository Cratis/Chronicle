// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_a_step_fails;

public class and_it_threw_without_a_result : given.a_catch_up_observer_job
{
    Exception _error;

    void Establish() => _stateStorage.State.LastHandledEventSequenceNumber = 3UL;

    async Task Because()
    {
        await _job.Start(_request);
        var result = JobStepResult.Failed(new PerformJobStepError(string.Empty, ["Handler threw"], string.Empty, false));
        _error = await Catch.Exception(() => _job.RecordStepForTesting(JobStepId.New(), result));
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.FailedSteps = 1;
        await _job.CompleteForTesting();
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] async Task should_reach_terminal_handling_without_claiming_progress() =>
        await _observer.Received(1).CaughtUp(_jobId, (EventSequenceNumber)3UL, EventSequenceNumber.Unavailable);
}
