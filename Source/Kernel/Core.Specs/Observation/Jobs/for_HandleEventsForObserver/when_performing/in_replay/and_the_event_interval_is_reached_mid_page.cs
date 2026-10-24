// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_the_event_interval_is_reached_mid_page : given.a_job_step_with_a_replay_event_interval
{
    void Establish() => SetupPages(AlternatingEvents(1, 5));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_flush_at_the_start_every_interval_and_at_the_end() => _flushes.Count.ShouldEqual(4);
    [Fact] void should_flush_in_replay_mode() => _flushes.TrueForAll(replay => replay).ShouldBeTrue();
    [Fact] void should_report_a_batch_per_interval_and_the_page_remainder() => _reports.Count.ShouldEqual(3);
    [Fact] void should_report_each_interval_batch_after_its_checkpoint_is_durable() => string.Join(',', _durableAtReport.Take(2).Select(number => number.Value)).ShouldEqual("2,4");
    [Fact] void should_report_the_page_remainder_before_the_debounced_page_end_checkpoint() => _durableAtReport[2].ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_leave_the_final_position_durable() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
}
