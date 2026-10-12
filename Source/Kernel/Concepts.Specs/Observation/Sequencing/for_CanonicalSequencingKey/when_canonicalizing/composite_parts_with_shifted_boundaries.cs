// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class composite_parts_with_shifted_boundaries : Specification
{
    CanonicalSequencingKey _first;
    CanonicalSequencingKey _second;

    void Because()
    {
        _first = CanonicalSequencingKey.From(given.golden_keys.Key(5));
        _second = CanonicalSequencingKey.From(given.golden_keys.Key(6));
    }

    [Fact] void should_keep_the_canonical_keys_distinct() => (_first == _second).ShouldBeFalse();
    [Fact] void should_keep_the_hashes_distinct() => (SequencingKeyHash.From(_first) == SequencingKeyHash.From(_second)).ShouldBeFalse();
}
