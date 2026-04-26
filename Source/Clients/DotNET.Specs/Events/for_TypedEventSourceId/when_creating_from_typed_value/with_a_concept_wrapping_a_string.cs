// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_creating_from_typed_value;

public class with_a_concept_wrapping_a_string : Specification
{
    static readonly StringConcept _input = new("some-concept-id");
    EventSourceId<StringConcept> _result;

    void Because() => _result = _input;

    [Fact] void should_have_the_concept_string_value_as_value() => ((EventSourceId)_result).Value.ShouldEqual(_input.Value);
    [Fact] void should_have_the_concept_as_typed_value() => _result.TypedValue.ShouldEqual(_input);
}
