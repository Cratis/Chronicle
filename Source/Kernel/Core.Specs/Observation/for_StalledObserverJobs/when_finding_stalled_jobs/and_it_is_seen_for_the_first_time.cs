// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_it_is_seen_for_the_first_time : given.a_stalled_job_tracker
{
    void Because() => _result = _tracker.Find([_job], _now);
    [Fact] void should_not_return_the_job() => _result.ShouldBeEmpty();
}
