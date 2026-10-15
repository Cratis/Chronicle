// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class an_nfc_and_an_nfd_form_of_the_same_text : Specification
{
    CanonicalSequencingKey _first;
    CanonicalSequencingKey _second;

    void Because()
    {
        _first = CanonicalSequencingKey.From(given.golden_keys.Key(2));
        _second = CanonicalSequencingKey.From(given.golden_keys.Key(3));
    }

    [Fact] void should_keep_the_canonical_keys_distinct() => (_first == _second).ShouldBeFalse();
    [Fact] void should_keep_the_hashes_distinct() => (SequencingKeyHash.From(_first) == SequencingKeyHash.From(_second)).ShouldBeFalse();
}
