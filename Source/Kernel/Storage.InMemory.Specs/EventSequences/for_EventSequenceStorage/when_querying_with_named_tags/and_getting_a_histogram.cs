// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_getting_a_histogram : given.a_storage_with_named_tags
{
    IEnumerable<HistogramBucket> _buckets;

    async Task Because() => _buckets = await _storage.GetHistogram(HistogramResolution.Day, new EventSequenceQueryCriteria(EventSourceId: _firstEventSourceId)
    {
        NamedTags = [new(new TagName("account"), ["one", "three"])]
    });

    [Fact] void should_include_only_days_with_matching_events() => _buckets.Select(_ => _.Occurred).ShouldEqual([new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero)]);
    [Fact] void should_count_only_matching_events() => _buckets.Select(_ => _.Count).ShouldEqual([2L]);
}
