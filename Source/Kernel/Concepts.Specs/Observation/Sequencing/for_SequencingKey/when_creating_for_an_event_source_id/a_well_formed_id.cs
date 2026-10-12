// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.when_creating_for_an_event_source_id;

public class a_well_formed_id : Specification
{
    EventSourceIdSequencingKey _key;
    CanonicalSequencingKey _canonical;

    void Because()
    {
        _key = SequencingKey.ForEventSourceId("abc");
        _canonical = CanonicalSequencingKey.From(_key);
    }

    [Fact] void should_retain_the_original_id() => _key.EventSourceId.Value.ShouldEqual("abc");
    [Fact] void should_use_the_value_kind() => _canonical.Describe().Kind.ShouldEqual(SequencingKeyKind.Value);
    [Fact] void should_preserve_the_value_bytes() => Convert.ToHexStringLower(_canonical.Span).ShouldEqual("0000000003616263");
}
