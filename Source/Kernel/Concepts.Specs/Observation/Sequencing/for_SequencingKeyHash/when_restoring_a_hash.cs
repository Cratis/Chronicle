// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyHash;

public class when_restoring_a_hash : Specification
{
    SequencingKeyHash _original;
    SequencingKeyHash _restored;

    void Establish() => _original = SequencingKeyHash.From(CanonicalSequencingKey.From(given.golden_keys.Key(0)));
    void Because() => _restored = SequencingKeyHash.FromStored(_original.Span);

    [Fact] void should_compare_by_bytes() => _restored.ShouldEqual(_original);
    [Fact] void should_have_the_same_value_hash_code() => _restored.GetHashCode().ShouldEqual(_original.GetHashCode());
}
