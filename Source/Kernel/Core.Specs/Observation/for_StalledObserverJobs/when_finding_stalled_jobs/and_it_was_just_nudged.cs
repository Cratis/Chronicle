// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_it_was_just_nudged : given.a_stalled_job_tracker
{
    void Establish()
    {
        _tracker.Find([_job], _now);
        _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter).ShouldContainOnly(_job.Id);
    }

    void Because() => _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter + TimeSpan.FromMinutes(1));
    [Fact] void should_not_return_the_job() => _result.ShouldBeEmpty();
    [Fact] void should_return_the_job_after_another_period() => _tracker.Find([_job], _now + (JobLiveness.ResumeStalledAfter * 2)).ShouldContainOnly(_job.Id);
}
