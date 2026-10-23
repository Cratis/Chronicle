// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_the_event_interval_is_disabled : given.a_job_step_with_a_replay_event_interval
{
    protected override int ReplayCheckpointEventInterval => 0;

    void Establish() => SetupPages(AlternatingEvents(1, 5));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_only_flush_at_the_start_and_the_end() => _flushes.Count.ShouldEqual(2);
    [Fact] void should_report_one_batch() => _reports.Count.ShouldEqual(1);
}
