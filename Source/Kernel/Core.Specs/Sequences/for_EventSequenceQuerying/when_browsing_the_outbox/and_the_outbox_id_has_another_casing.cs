// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_browsing_the_outbox;

public class and_the_outbox_id_has_another_casing : given.an_outbox_with_a_mixed_catalog
{
    IEventSequenceStorage _upper;
    IEnumerable<SequenceHistogramBucket> _buckets;

    void Establish()
    {
        _upper = _storage.GetEventStore((EventStoreName)"store").GetNamespace((EventStoreNamespaceName)"default").GetEventSequence((Concepts.EventSequences.EventSequenceId)"OUTBOX");
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(false);
        _upper.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns((Concepts.Events.EventCount)5UL);
        _upper.GetPage(Arg.Any<EventSequenceQueryCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>()).Returns(cursor);
        _upper.GetHistogram(Arg.Any<HistogramResolution>(), Arg.Any<EventSequenceQueryCriteria>()).Returns([new HistogramBucket(DateTimeOffset.UnixEpoch, 5)]);
    }

    async Task Because()
    {
        await EventSequenceQuerying.QueryPage(_storage, _compliance, "store", "default", "OUTBOX", EventSequenceQueryCriteria.Empty, 0, 10, EventSequenceQuerySort.Default);
        _buckets = await EventSequenceQuerying.Histogram(_storage, "store", "default", "OUTBOX", HistogramResolution.Day, EventSequenceQueryCriteria.Empty);
    }

    [Fact] void should_count_only_public_types() => _upper.Received(1).GetCountMatching(Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)));
    [Fact] void should_page_only_public_types() => _upper.Received(1).GetPage(Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)), 0, 10, Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>());
    [Fact] void should_read_the_histogram_for_public_types() => _upper.Received(1).GetHistogram(HistogramResolution.Day, Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)));
    [Fact] void should_return_the_buckets() => _buckets.Count().ShouldEqual(1);
}
