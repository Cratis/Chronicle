// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_marking_a_job_for_retry;

public class and_it_is_not_tracked : given.a_stalled_job_tracker
{
    void Because()
    {
        _tracker.RetryOnNextTick(_job.Id);
        _result = _tracker.Find([], _now);
    }

    [Fact] void should_not_return_anything() => _result.ShouldBeEmpty();
}
