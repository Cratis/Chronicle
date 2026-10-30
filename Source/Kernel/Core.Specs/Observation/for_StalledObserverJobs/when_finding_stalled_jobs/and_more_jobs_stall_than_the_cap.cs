// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_more_jobs_stall_than_the_cap : given.a_stalled_job_tracker
{
    JobState[] _jobs;

    void Establish()
    {
        _jobs = Enumerable.Range(0, StalledObserverJobs.MaxResumesPerTick + 2)
            .Select(_ => new JobState { Id = JobId.New(), Status = JobStatus.Running }).ToArray();
        for (var index = 0; index < _jobs.Length; index++)
        {
            _tracker.Find(_jobs.Take(index + 1), _now + TimeSpan.FromSeconds(index));
        }
    }

    void Because() => _result = _tracker.Find(_jobs.Reverse(), _now + JobLiveness.ResumeStalledAfter + TimeSpan.FromMinutes(1));
    [Fact] void should_limit_the_number_of_resumes() => _result.Count.ShouldEqual(StalledObserverJobs.MaxResumesPerTick);
    [Fact] void should_return_the_oldest_jobs_first() => _result.SequenceEqual(_jobs.Take(StalledObserverJobs.MaxResumesPerTick).Select(_ => _.Id)).ShouldBeTrue();
}
