// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_the_job_is_removing : given.a_stalled_job_tracker
{
    void Establish()
    {
        _tracker.Find([_job], _now);
        _job.Status = JobStatus.Removing;
    }

    void Because() => _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter);
    [Fact] void should_not_return_the_job() => _result.ShouldBeEmpty();
}
