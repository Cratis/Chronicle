// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_browsing_the_outbox;

public class and_only_private_types_are_requested : given.an_outbox_with_a_mixed_catalog
{
    ulong _count;
    IEnumerable<Concepts.Events.AppendedEvent> _events;
    IEnumerable<SequenceHistogramBucket> _buckets;

    async Task Because()
    {
        var criteria = EventSequenceQueryCriteria.Empty with { EventTypes = [new Concepts.Events.EventType("private", EventTypeGeneration.First)] };
        (_events, _count) = await EventSequenceQuerying.QueryPage(_storage, _compliance, "store", "default", "outbox", criteria, 0, 10, EventSequenceQuerySort.Default);
        _buckets = await EventSequenceQuerying.Histogram(_storage, "store", "default", "outbox", HistogramResolution.Day, criteria);
    }

    [Fact] void should_return_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_report_zero() => _count.ShouldEqual(0UL);
    [Fact] void should_return_no_buckets() => _buckets.ShouldBeEmpty();
    [Fact] void should_not_count_in_storage() => _sequence.DidNotReceiveWithAnyArgs().GetCountMatching(default!);
    [Fact] void should_not_page_in_storage() => _sequence.DidNotReceiveWithAnyArgs().GetPage(default!, default, default, default, default);
    [Fact] void should_not_read_the_histogram_in_storage() => _sequence.DidNotReceiveWithAnyArgs().GetHistogram(default, default!);
}
