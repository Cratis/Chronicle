// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_a_page_ends_below_the_event_interval : given.a_job_step_with_a_replay_event_interval
{
    protected override int ReplayCheckpointEventInterval => 10;

    void Establish() => SetupPages(AlternatingEvents(1, 2), AlternatingEvents(3, 4));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_only_flush_at_the_start_and_the_end() => _flushes.Count.ShouldEqual(2);
    [Fact] void should_still_report_each_page() => _reports.Count.ShouldEqual(2);
}
