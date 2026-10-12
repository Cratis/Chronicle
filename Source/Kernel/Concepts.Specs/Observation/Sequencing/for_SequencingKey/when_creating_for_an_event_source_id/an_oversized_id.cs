// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GoldenKeys = Cratis.Chronicle.Concepts.Observation.Sequencing.given.golden_keys;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.when_creating_for_an_event_source_id;

public class an_oversized_id : Specification
{
    string _id;
    EventSourceIdSequencingKey _key;
    CanonicalSequencingKey _canonical;

    void Establish() => _id = new string('a', 8188);
    void Because()
    {
        _key = SequencingKey.ForEventSourceId(_id);
        _canonical = CanonicalSequencingKey.From(_key);
    }

    [Fact] void should_retain_the_original_id() => _key.EventSourceId.Value.ShouldEqual(_id);
    [Fact] void should_use_the_event_source_id_digest_kind() => _canonical.Describe().Kind.ShouldEqual(SequencingKeyKind.EventSourceIdDigest);
    [Fact] void should_have_a_bounded_length() => _canonical.Span.Length.ShouldEqual(33);
    [Fact] void should_preserve_the_exact_digest_bytes() => Convert.ToHexStringLower(_canonical.Span).ShouldEqual((string)GoldenKeys.Rows().ElementAt(16)[1]);
}
