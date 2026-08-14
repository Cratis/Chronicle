// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_getting_a_histogram;

public class and_the_resolution_is_per_day : given.a_storage_with_events_spread_over_time
{
    IEnumerable<HistogramBucket> _buckets;

    async Task Because() => _buckets = await _storage.GetHistogram(HistogramResolution.Day, EventSequenceQueryCriteria.Empty);

    [Fact] void should_produce_one_bucket_per_day() => _buckets.Count().ShouldEqual(3);
    [Fact] void should_order_the_buckets_oldest_first() => _buckets.Select(_ => _.Occurred).ShouldEqual(_buckets.Select(_ => _.Occurred).Order());
    [Fact] void should_truncate_each_bucket_to_midnight() => _buckets.Select(_ => _.Occurred.TimeOfDay).ShouldEqual([TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero]);
    [Fact] void should_count_every_event_in_its_day() => _buckets.Select(_ => _.Count).ShouldEqual([2L, 2L, 2L]);
    [Fact] void should_start_at_the_first_day() => _buckets.First().Occurred.ShouldEqual(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));
}
