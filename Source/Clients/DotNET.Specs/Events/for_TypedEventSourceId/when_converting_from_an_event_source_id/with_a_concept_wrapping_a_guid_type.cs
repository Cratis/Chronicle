// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_converting_from_an_event_source_id;

public class with_a_concept_wrapping_a_guid_type : Specification
{
    static readonly Guid _expected = Guid.Parse("b4e2b6a0-1b3e-4e6a-8c4d-2f7e9b1a3c5d");
    EventSourceId<GuidConcept> _result;

    void Because() => _result = EventSourceId<GuidConcept>.From(new EventSourceId(_expected.ToString()));

    [Fact] void should_have_the_guid_string_as_value() => _result.Value.ShouldEqual(_expected.ToString());
    [Fact] void should_have_the_concept_with_the_parsed_guid_as_typed_value() => _result.TypedValue.ShouldEqual(new GuidConcept(_expected));
}
