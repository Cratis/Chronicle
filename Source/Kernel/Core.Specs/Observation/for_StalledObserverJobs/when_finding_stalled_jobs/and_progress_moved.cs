// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_progress_moved : given.a_stalled_job_tracker
{
    void Establish()
    {
        _tracker.Find([_job], _now);
        _job.Progress.SuccessfulSteps = 1;
    }

    void Because()
    {
        _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter);
        _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter + TimeSpan.FromMinutes(1)).ShouldBeEmpty();
    }

    [Fact] void should_not_return_the_job() => _result.ShouldBeEmpty();
}
