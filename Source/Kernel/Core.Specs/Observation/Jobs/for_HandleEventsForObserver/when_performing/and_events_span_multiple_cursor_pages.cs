// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_events_span_multiple_cursor_pages : given.a_performing_job_step
{
    void Establish()
    {
        // The cursor delivers the range across two pages, with the module partition continuing on the
        // second page. Global sequence order must hold across pages, not just within a single page.
        _eventCursor.Current.Returns(
            [
                CreateEvent(1UL, "module"),
                CreateEvent(2UL, "feature")
            ],
            [
                CreateEvent(3UL, "module")
            ]);
        var moveNextCount = 0;
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(moveNextCount++ < 2));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_handle_three_consecutive_partition_batches_across_pages() => _handledBatches.Count.ShouldEqual(3);
    [Fact] void should_handle_module_event_one_first() => _handledBatches[0].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_handle_feature_event_two_second() => _handledBatches[1].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_handle_module_event_three_from_the_next_page_last() => _handledBatches[2].SequenceNumbers[0].ShouldEqual((EventSequenceNumber)3UL);
}
