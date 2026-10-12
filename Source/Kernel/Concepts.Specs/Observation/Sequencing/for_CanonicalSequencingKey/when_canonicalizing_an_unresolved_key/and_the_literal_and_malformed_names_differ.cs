// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class and_the_literal_and_malformed_names_differ : Specification
{
    CanonicalSequencingKey _literal;
    CanonicalSequencingKey _malformed;

    void Because()
    {
        _literal = CanonicalSequencingKey.From(given.golden_keys.Key(10));
        _malformed = CanonicalSequencingKey.From(given.golden_keys.Key(11));
    }

    [Fact] void should_keep_the_domains_distinct() => (_literal == _malformed).ShouldBeFalse();
    [Fact] void should_keep_the_hashes_distinct() => (SequencingKeyHash.From(_literal) == SequencingKeyHash.From(_malformed)).ShouldBeFalse();
}
