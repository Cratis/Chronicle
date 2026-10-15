// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class a_valid_surrogate_pair : Specification
{
    CanonicalSequencingKey _key;

    void Because() => _key = CanonicalSequencingKey.From(given.golden_keys.Key(4));

    [Fact] void should_preserve_the_exact_canonical_bytes() => Convert.ToHexStringLower(_key.Span).ShouldEqual((string)given.golden_keys.Rows().ElementAt(4)[1]);
    [Fact] void should_describe_the_ordinal_part() => _key.Describe().Parts[0].ShouldEqual(((EventSourceIdSequencingKey)given.golden_keys.Key(4)).EventSourceId.Value);
}
