// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_JobLiveness;

public class when_checking_for_abandoned_preparation : Specification
{
    static readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    bool _longPreparingWithoutSteps;
    bool _recentlyPreparingWithoutSteps;
    bool _longPreparingWithSteps;
    bool _stopped;

    void Because()
    {
        _longPreparingWithoutSteps = JobLiveness.PreparationAbandoned(Job(JobStatus.StartingSteps, 0, _now - TimeSpan.FromHours(1)), _now);
        _recentlyPreparingWithoutSteps = JobLiveness.PreparationAbandoned(Job(JobStatus.StartingSteps, 0, _now - TimeSpan.FromMinutes(1)), _now);
        _longPreparingWithSteps = JobLiveness.PreparationAbandoned(Job(JobStatus.StartingSteps, 100, _now - TimeSpan.FromHours(1)), _now);
        _stopped = JobLiveness.PreparationAbandoned(Job(JobStatus.Stopped, 0, _now - TimeSpan.FromHours(1)), _now);
    }

    [Fact] void should_abandon_a_job_that_prepared_nothing_for_a_long_time() => _longPreparingWithoutSteps.ShouldBeTrue();
    [Fact] void should_not_abandon_a_job_that_just_started_preparing() => _recentlyPreparingWithoutSteps.ShouldBeFalse();
    [Fact] void should_not_abandon_a_job_that_has_steps() => _longPreparingWithSteps.ShouldBeFalse();
    [Fact] void should_not_abandon_a_stopped_job() => _stopped.ShouldBeFalse();

    static JobState Job(JobStatus status, int totalSteps, DateTimeOffset since) => new()
    {
        Id = JobId.New(),
        Status = status,
        Created = since,
        Progress = new JobProgress { TotalSteps = totalSteps },
        StatusChanges = [new JobStatusChanged { Status = status, Occurred = since }]
    };
}
