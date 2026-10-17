// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobLiveness;

public class when_checking_a_job_recorded_as_running : Specification
{
    static readonly DateTimeOffset _now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    bool _stalledLongAgo;
    bool _concludedJustNow;
    bool _stepsInFlight;
    bool _noStepsYet;
    bool _stopped;

    void Because()
    {
        _stalledLongAgo = JobLiveness.HasConcludedAllStepsLongAgo(A(JobStatus.Running, 3, 3, _now - TimeSpan.FromHours(1)), _now);
        _concludedJustNow = JobLiveness.HasConcludedAllStepsLongAgo(A(JobStatus.Running, 3, 3, _now - TimeSpan.FromSeconds(5)), _now);
        _stepsInFlight = JobLiveness.HasConcludedAllStepsLongAgo(A(JobStatus.Running, 3, 2, _now - TimeSpan.FromHours(1)), _now);
        _noStepsYet = JobLiveness.HasConcludedAllStepsLongAgo(A(JobStatus.Running, 0, 0, _now - TimeSpan.FromHours(1)), _now);
        _stopped = JobLiveness.HasConcludedAllStepsLongAgo(A(JobStatus.Stopped, 3, 3, _now - TimeSpan.FromHours(1)), _now);
    }

    [Fact] void should_consider_a_job_whose_steps_all_concluded_long_ago_lost() => _stalledLongAgo.ShouldBeTrue();
    [Fact] void should_leave_a_job_that_is_still_finalizing_alone() => _concludedJustNow.ShouldBeFalse();
    [Fact] void should_leave_a_job_with_steps_in_flight_alone() => _stepsInFlight.ShouldBeFalse();
    [Fact] void should_leave_a_job_that_has_no_steps_yet_alone() => _noStepsYet.ShouldBeFalse();
    [Fact] void should_leave_a_job_that_is_not_running_alone() => _stopped.ShouldBeFalse();

    static JobState A(JobStatus status, int total, int concluded, DateTimeOffset lastChange) => new()
    {
        Id = JobId.New(),
        Status = status,
        Progress = new() { TotalSteps = total, SuccessfulSteps = concluded },
        StatusChanges = [new JobStatusChanged { Status = status, Occurred = lastChange }]
    };
}
