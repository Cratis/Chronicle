// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey;

public class when_copying_supported_keys : Specification
{
    ValueSequencingKey _value;
    UnresolvedSequencingKey _unresolved;

    void Because()
    {
        _value = SequencingKey.ForEventSourceId("abc") with { };
        _unresolved = SequencingKey.Unresolved("\uD800x") with { };
    }

    [Fact] void should_preserve_the_value_parts() => _value.ShouldEqual(SequencingKey.ForEventSourceId("abc"));
    [Fact] void should_preserve_the_unresolved_event_type() => _unresolved.ShouldEqual(SequencingKey.Unresolved("\uD800x"));
}
