// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class with_the_unknown_event_type : Specification
{
    CanonicalSequencingKey _key;

    void Because() => _key = CanonicalSequencingKey.From(given.golden_keys.Key(9));

    [Fact] void should_preserve_the_exact_canonical_bytes() => Convert.ToHexStringLower(_key.Span).ShouldEqual((string)given.golden_keys.Rows().ElementAt(9)[1]);
    [Fact] void should_select_the_deterministic_form() => _key.Span[1].ShouldEqual((byte)UnresolvedKeyForm.Text);
    [Fact] void should_describe_the_original_text() => _key.Describe().Text.ShouldEqual(((UnresolvedSequencingKey)given.golden_keys.Key(9)).EventType.Value);
}
