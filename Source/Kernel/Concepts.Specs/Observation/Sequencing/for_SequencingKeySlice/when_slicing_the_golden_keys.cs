// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeySlice;

public class when_slicing_the_golden_keys : Specification
{
    [Theory]
    [MemberData(nameof(given.golden_keys.TheoryRows), MemberType = typeof(given.golden_keys))]
    public void should_match_all_xxhash32_and_slice_vectors(int index, string canonicalHex, string hashHex, uint hash, ushort slice)
    {
        var key = CanonicalSequencingKey.From(given.golden_keys.Key(index));
        Convert.ToHexStringLower(key.Span).ShouldEqual(canonicalHex);
        SequencingKeyHash.From(key).ToString().ShouldEqual(hashHex);
        SequencingKeySliceHash.Hash(key.Span).ShouldEqual(hash);
        SequencingKeySlice.From(key).Value.ShouldEqual(slice);
        (SequencingKeySlice.From(key).Value < SequencingKeySlice.Count).ShouldBeTrue();
    }
}
