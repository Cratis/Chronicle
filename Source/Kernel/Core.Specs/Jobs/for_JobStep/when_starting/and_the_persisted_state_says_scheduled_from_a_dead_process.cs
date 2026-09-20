// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Jobs.for_JobStep.when_starting;

/// <summary>
/// The wedge this guards against: a step that was Scheduled when its silo died must start over on
/// the next Start, not answer AlreadyStarted - nothing is running in the fresh activation, so
/// AlreadyStarted leaves the step Scheduled forever, its job permanently alive without ever running,
/// and an observer whose catch-up waits on that job stranding until quarantine.
/// </summary>
public class and_the_persisted_state_says_scheduled_from_a_dead_process : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _result;

    async Task Because() => _result = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));

    [Fact] void should_start_rather_than_answer_already_started() => _result.IsSuccess.ShouldBeTrue();

    [Fact]
    async Task should_perform_the_step_again()
    {
        var performed = await Task.WhenAny(_jobStep.Performed.Task, Task.Delay(TimeSpan.FromSeconds(5), TimeProvider.System));
        performed.ShouldEqual(_jobStep.Performed.Task);
    }
}
