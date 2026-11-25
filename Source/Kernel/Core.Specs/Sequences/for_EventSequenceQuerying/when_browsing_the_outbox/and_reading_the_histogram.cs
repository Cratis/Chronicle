// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_browsing_the_outbox;

public class and_reading_the_histogram : given.an_outbox_with_a_mixed_catalog
{
    IEnumerable<SequenceHistogramBucket> _buckets;

    async Task Because() => _buckets = await EventSequenceQuerying.Histogram(_storage, "store", "default", "outbox", HistogramResolution.Day, EventSequenceQueryCriteria.Empty);

    [Fact] void should_read_only_public_types() => _sequence.Received(1).GetHistogram(HistogramResolution.Day, Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)));
    [Fact] void should_return_the_buckets() => _buckets.Count().ShouldEqual(1);
}
