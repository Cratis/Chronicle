// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_hashing_known_identifiers : Specification
{
    (string Id, ulong Hash)[] _vectors;
    ReplayBucket[] _buckets;

    void Establish() => _vectors =
    [
        (string.Empty, 0xcbf29ce484222325UL),
        ("a", 0xaf63dc4c8601ec8cUL),
        ("foobar", 0x85944171f73967e8UL),
        ("café", 0x48e8823acfa40d89UL),
        ("01234567-89ab-cdef-0123-456789abcdef", 0x3d53a23762e9e0a5UL)
    ];

    void Because() => _buckets = _vectors
        .SelectMany(vector => Enumerable.Range(1, 64).Select(count => ReplayBuckets.For(vector.Id, count)))
        .ToArray();

    [Fact] void should_match_pinned_fnv1a_hashes_for_every_supported_count() => _buckets.Select(bucket => bucket.Index).ShouldEqual(_vectors.SelectMany(vector => Enumerable.Range(1, 64).Select(count => (int)(vector.Hash % (ulong)count))));
    [Fact] void should_preserve_the_requested_count() => _buckets.Select(bucket => bucket.Count).ShouldEqual(_vectors.SelectMany(_ => Enumerable.Range(1, 64)));
    [Fact] void should_use_hash_version_one() => _buckets.All(bucket => bucket.HashVersion == 1).ShouldBeTrue();
}
