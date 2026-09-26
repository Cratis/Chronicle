// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_getting_a_histogram : given.a_storage_with_named_tags
{
    IEnumerable<HistogramBucket> _buckets;

    async Task Because() => _buckets = await _storage.GetHistogram(HistogramResolution.Day, new() { NamedTags = [new(new TagName("account"))] });

    [Fact] void should_count_only_matching_events() => _buckets.Single().Count.ShouldEqual(2L);
}
