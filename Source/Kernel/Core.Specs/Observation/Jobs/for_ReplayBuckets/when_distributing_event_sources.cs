// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_distributing_event_sources : Specification
{
    ReplayBucket[] _buckets;

    void Because() => _buckets = Enumerable.Range(0, 4096)
        .Select(index => ReplayBuckets.For($"order-{index.ToString(CultureInfo.InvariantCulture)}", 64))
        .ToArray();

    [Fact] void should_keep_every_index_in_range() => _buckets.All(bucket => bucket.Index is >= 0 and < 64).ShouldBeTrue();
    [Fact] void should_populate_every_bucket() => _buckets.Select(bucket => bucket.Index).Distinct().Count().ShouldEqual(64);
    [Fact] void should_spread_sources_roughly_evenly() => _buckets.GroupBy(bucket => bucket.Index).All(group => group.Count() is >= 32 and <= 96).ShouldBeTrue();
}
