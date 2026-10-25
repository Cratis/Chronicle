// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_a_job_is_no_longer_listed : given.a_stalled_job_tracker
{
    void Establish()
    {
        _tracker.Find([_job], _now);
        _tracker.Find([], _now + JobLiveness.ResumeStalledAfter);
    }

    void Because() => _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter);
    [Fact] void should_start_a_fresh_mark() => _result.ShouldBeEmpty();
}
