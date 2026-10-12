// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_CanonicalSequencingKey.when_canonicalizing_an_unresolved_key;

public class and_the_event_source_id_and_event_type_digests_differ : Specification
{
    CanonicalSequencingKey _eventSourceId;
    CanonicalSequencingKey _eventType;

    void Because()
    {
        _eventSourceId = CanonicalSequencingKey.From(SequencingKey.ForEventSourceId("\uD800x"));
        _eventType = CanonicalSequencingKey.From(SequencingKey.Unresolved("\uD800x"));
    }

    [Fact] void should_share_the_raw_code_unit_digest() => _eventSourceId.Describe().DigestHex.ShouldEqual(_eventType.Describe().DigestHex);
    [Fact] void should_have_distinct_canonical_bytes() => _eventSourceId.ShouldNotEqual(_eventType);
    [Fact] void should_have_distinct_identity_hashes() => SequencingKeyHash.From(_eventSourceId).ShouldNotEqual(SequencingKeyHash.From(_eventType));
    [Fact] void should_separate_the_event_source_domain() => _eventSourceId.Span[0].ShouldEqual((byte)2);
    [Fact] void should_separate_the_unresolved_domain() => Convert.ToHexStringLower(_eventType.Span[..2]).ShouldEqual("0101");
}
