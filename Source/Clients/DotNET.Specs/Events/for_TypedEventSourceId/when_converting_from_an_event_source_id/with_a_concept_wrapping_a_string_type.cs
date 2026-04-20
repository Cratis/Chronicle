// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_converting_from_an_event_source_id;

public class with_a_concept_wrapping_a_string_type : Specification
{
    const string _input = "some-concept-id";
    EventSourceId<StringConcept> _result;

    void Because() => _result = EventSourceId<StringConcept>.From(new EventSourceId(_input));

    [Fact] void should_have_the_string_as_value() => _result.Value.ShouldEqual(_input);
    [Fact] void should_have_the_concept_with_the_string_as_typed_value() => _result.TypedValue.ShouldEqual(new StringConcept(_input));
}
