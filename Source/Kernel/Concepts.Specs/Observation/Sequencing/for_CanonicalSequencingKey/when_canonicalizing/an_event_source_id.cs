// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing;

public class an_event_source_id : Specification
{
    CanonicalSequencingKey _key;

    void Because() => _key = CanonicalSequencingKey.From(given.golden_keys.Key(0));

    [Fact] void should_preserve_the_exact_canonical_bytes() => Convert.ToHexStringLower(_key.Span).ShouldEqual((string)given.golden_keys.Rows().ElementAt(0)[1]);
    [Fact] void should_describe_the_ordinal_part() => _key.Describe().Parts[0].ShouldEqual(((EventSourceIdSequencingKey)given.golden_keys.Key(0)).EventSourceId.Value);
}
