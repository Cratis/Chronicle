// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_progress_has_not_moved_for_the_stall_period : given.a_stalled_job_tracker
{
    void Establish() => _tracker.Find([_job], _now);
    void Because() => _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter);
    [Fact] void should_return_the_job() => _result.ShouldContainOnly(_job.Id);
}
