// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_progress_moves_after_a_later_attempt : given.a_stalled_job_tracker
{
    JobState[] _jobs;
    IReadOnlyList<JobId> _afterProgress;

    void Establish()
    {
        _jobs = Enumerable.Range(0, StalledObserverJobs.MaxResumesPerTick + 2)
            .Select(_ => new JobState { Id = JobId.New(), Status = JobStatus.Running }).ToArray();
        _tracker.Find(_jobs, _now);
        _now += JobLiveness.ResumeStalledAfter;
        _tracker.Find(_jobs, _now);
        _tracker.Find(_jobs, _now + TimeSpan.FromSeconds(1));
        _jobs[^2].Progress = new() { SuccessfulSteps = 1 };
        _afterProgress = _tracker.Find(_jobs, _now + TimeSpan.FromSeconds(2));
        foreach (var job in _jobs.Where(_ => _ != _jobs[^2]))
        {
            _tracker.RetryOnNextTick(job.Id);
        }
    }

    void Because() => _result = _tracker.Find(_jobs, _now + JobLiveness.ResumeStalledAfter + TimeSpan.FromSeconds(2));
    [Fact] void should_reset_the_stall_timer_when_progress_moves() => _afterProgress.ShouldBeEmpty();
    [Fact] void should_preserve_attempt_priority_when_progress_moves() => _result.SequenceEqual(_jobs.Take(StalledObserverJobs.MaxResumesPerTick).Select(_ => _.Id)).ShouldBeTrue();
}
