// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.when_creating_for_an_event_source_id;

public class an_unpaired_surrogate_id : Specification
{
    EventSourceIdSequencingKey _key;
    CanonicalSequencingKey _canonical;

    void Because()
    {
        _key = SequencingKey.ForEventSourceId("\uD800x");
        _canonical = CanonicalSequencingKey.From(_key);
    }

    [Fact] void should_retain_the_original_code_units() => _key.EventSourceId.Value.ShouldEqual("\uD800x");
    [Fact] void should_use_the_event_source_id_digest_kind() => _canonical.Describe().Kind.ShouldEqual(SequencingKeyKind.EventSourceIdDigest);
    [Fact] void should_have_a_bounded_length() => _canonical.Span.Length.ShouldEqual(33);
    [Fact] void should_describe_the_raw_code_unit_digest() => _canonical.Describe().DigestHex.ShouldEqual("9466fd0a01cfa0678128b33a9829115f73a1845ab4aa3f5c9da6fe3c6e400db8");
}
