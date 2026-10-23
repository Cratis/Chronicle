// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_hashing_equivalent_guid_identifiers : Specification
{
    ReplayBucket[] _buckets;

    void Because() => _buckets = new[]
    {
        "01234567-89ab-cdef-0123-456789abcdef",
        "01234567-89AB-CDEF-0123-456789ABCDEF",
        "{01234567-89ab-cdef-0123-456789abcdef}",
        "0123456789abcdef0123456789abcdef"
    }.Select(id => ReplayBuckets.For(id, 63)).ToArray();

    [Fact] void should_assign_the_same_bucket() => _buckets.Distinct().Count().ShouldEqual(1);
    [Fact] void should_hash_canonical_network_order_guid_bytes() => _buckets[0].Index.ShouldEqual(25);
}
