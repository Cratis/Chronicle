// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Jobs.for_JobStep.when_starting;

/// <summary>
/// The double-start guard the resume fix must not lose: a step whose work is genuinely in flight in
/// this activation still answers AlreadyStarted.
/// </summary>
public class and_work_is_already_running_in_this_activation : given.a_job_step_left_scheduled_by_a_dead_process
{
    Result<StartJobStepError> _first;
    Result<StartJobStepError> _second;

    async Task Because()
    {
        _jobStep.HoldPerformOpen = true;
        _first = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        await Task.WhenAny(_jobStep.Performed.Task, Task.Delay(TimeSpan.FromSeconds(5), TimeProvider.System));
        _second = await _jobStep.Start(GrainId.Create("job", _jobId.ToString()));
        _jobStep.Gate.TrySetResult();
    }

    [Fact] void should_start_the_first_time() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_the_second_start() => ((StartJobStepError)_second).ShouldEqual(StartJobStepError.AlreadyStarted);
}
