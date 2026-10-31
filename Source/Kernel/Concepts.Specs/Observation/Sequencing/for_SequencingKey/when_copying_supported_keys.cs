// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKey;

public class when_copying_supported_keys : Specification
{
    ValueSequencingKey _value;
    EventSourceIdSequencingKey _eventSourceId;
    UnresolvedSequencingKey _unresolved;

    void Because()
    {
        _value = new ValueSequencingKey(["abc"]) with { };
        _eventSourceId = SequencingKey.ForEventSourceId("\uD800x") with { };
        _unresolved = SequencingKey.Unresolved("\uD800x") with { };
    }

    [Fact] void should_preserve_the_value_parts() => _value.ShouldEqual(new ValueSequencingKey(["abc"]));
    [Fact] void should_preserve_the_event_source_id() => _eventSourceId.ShouldEqual(SequencingKey.ForEventSourceId("\uD800x"));
    [Fact] void should_preserve_the_unresolved_event_type() => _unresolved.ShouldEqual(SequencingKey.Unresolved("\uD800x"));
}
