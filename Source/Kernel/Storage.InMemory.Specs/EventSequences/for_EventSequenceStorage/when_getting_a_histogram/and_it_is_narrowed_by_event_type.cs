// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_histogram;

/// <summary>
/// The histogram drives a time range picker that sits next to the other filters, so it has to
/// reflect the same narrowing the event list does - otherwise the picker offers ranges that
/// produce no rows.
/// </summary>
public class and_it_is_narrowed_by_event_type : given.a_storage_with_events_spread_over_time
{
    IEnumerable<HistogramBucket> _buckets;

    async Task Because() => _buckets = await _storage.GetHistogram(
        HistogramResolution.Day,
        new EventSequenceQueryCriteria(EventTypes: [_archived]));

    [Fact] void should_only_produce_buckets_containing_matching_events() => _buckets.Count().ShouldEqual(2);
    [Fact] void should_count_only_the_matching_events() => _buckets.Select(_ => _.Count).ShouldEqual([1L, 1L]);
    [Fact] void should_skip_the_day_without_a_matching_event() =>
        _buckets.Select(_ => _.Occurred).ShouldEqual(
        [
            new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero)
        ]);
}
