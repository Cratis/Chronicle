// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_marking_a_job_for_retry;

public class and_progress_moves_afterwards : given.a_stalled_job_tracker
{
    void Establish()
    {
        _tracker.Find([_job], _now);
        _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter).ShouldContainOnly(_job.Id);
        _tracker.RetryOnNextTick(_job.Id);
        _job.Progress = new() { TotalSteps = 10, SuccessfulSteps = 1 };
    }

    void Because() => _result = _tracker.Find([_job], _now + JobLiveness.ResumeStalledAfter);
    [Fact] void should_not_return_the_job() => _result.ShouldBeEmpty();
}
