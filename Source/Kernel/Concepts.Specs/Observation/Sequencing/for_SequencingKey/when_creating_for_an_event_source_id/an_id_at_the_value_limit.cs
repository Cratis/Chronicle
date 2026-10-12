// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GoldenKeys = Cratis.Chronicle.Concepts.Observation.Sequencing.given.golden_keys;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey.when_creating_for_an_event_source_id;

public class an_id_at_the_value_limit : Specification
{
    string _id;
    EventSourceIdSequencingKey _key;
    CanonicalSequencingKey _canonical;

    void Establish() => _id = new string('a', 8187);
    void Because()
    {
        _key = SequencingKey.ForEventSourceId(_id);
        _canonical = CanonicalSequencingKey.From(_key);
    }

    [Fact] void should_retain_the_original_id() => _key.EventSourceId.Value.ShouldEqual(_id);
    [Fact] void should_use_the_value_kind() => _canonical.Describe().Kind.ShouldEqual(SequencingKeyKind.Value);
    [Fact] void should_fill_the_canonical_limit() => _canonical.Span.Length.ShouldEqual(CanonicalSequencingKey.MaximumLength);
    [Fact] void should_preserve_the_existing_golden_vector() => Convert.ToHexStringLower(_canonical.Span).ShouldEqual((string)GoldenKeys.Rows().ElementAt(7)[1]);
}
