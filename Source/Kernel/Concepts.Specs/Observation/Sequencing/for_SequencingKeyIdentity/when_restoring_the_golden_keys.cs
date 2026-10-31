// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyIdentity;

public class when_restoring_the_golden_keys : Specification
{
    [Theory]
    [MemberData(nameof(given.golden_keys.TheoryRows), MemberType = typeof(given.golden_keys))]
    public void should_accept_consistent_typed_and_raw_columns(int index, string canonicalHex, string hashHex, uint hash, ushort slice)
    {
        var identity = SequencingKeyIdentity.For(given.golden_keys.Key(index));
        Convert.ToHexStringLower(identity.Key.Span).ShouldEqual(canonicalHex);
        identity.Hash.ToString().ShouldEqual(hashHex);
        SequencingKeySliceHash.Hash(identity.Key.Span).ShouldEqual(hash);
        identity.Slice.Value.ShouldEqual(slice);
        SequencingKeyIdentity.FromStored(identity.Kind, identity.Hash, identity.Key, identity.Slice).ShouldEqual(identity);
        SequencingKeyIdentity.FromStored(identity.Kind, identity.Hash.Span, identity.Key.Span, identity.Slice.Value).ShouldEqual(identity);
    }
}
