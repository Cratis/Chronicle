// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_resume_failures_fill_the_cap : given.a_stalled_job_tracker
{
    JobState[] _jobs;
    IReadOnlyList<JobId> _firstRound;

    void Establish()
    {
        _jobs = Enumerable.Range(0, StalledObserverJobs.MaxResumesPerTick + 2)
            .Select(_ => new JobState { Id = JobId.New(), Status = JobStatus.Running }).ToArray();
        _tracker.Find(_jobs, _now);
        _now += JobLiveness.ResumeStalledAfter;
        _firstRound = _tracker.Find(_jobs, _now);
        foreach (var id in _firstRound)
        {
            _tracker.RetryOnNextTick(id);
        }
    }

    void Because() => _result = _tracker.Find(_jobs.Reverse(), _now + TimeSpan.FromSeconds(1));
    [Fact] void should_keep_the_resume_cap() => _result.Count.ShouldEqual(StalledObserverJobs.MaxResumesPerTick);
    [Fact] void should_attempt_the_remaining_jobs_first() => _result.Take(2).SequenceEqual(_jobs.Skip(StalledObserverJobs.MaxResumesPerTick).Select(_ => _.Id)).ShouldBeTrue();
    [Fact] void should_retry_failed_jobs_in_the_remaining_capacity() => _result.Skip(2).SequenceEqual(_firstRound.Take(StalledObserverJobs.MaxResumesPerTick - 2)).ShouldBeTrue();
}
