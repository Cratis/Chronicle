// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_EventSourceIdJsonConverter.when_reading;

public class with_a_concept_wrapping_a_guid : Specification
{
    static readonly Guid _guid = Guid.Parse("b4e2b6a0-1b3e-4e6a-8c4d-2f7e9b1a3c5d");
    JsonSerializerOptions _options;
    EventSourceId<GuidConcept> _result;

    void Establish() => _options = new JsonSerializerOptions { Converters = { new EventSourceIdJsonConverterFactory() } };

    void Because() => _result = JsonSerializer.Deserialize<EventSourceId<GuidConcept>>($"\"{_guid}\"", _options)!;

    [Fact] void should_have_the_concept_with_the_parsed_guid_as_typed_value() => _result.TypedValue.ShouldEqual(new GuidConcept(_guid));
    [Fact] void should_have_the_guid_string_as_event_source_id_value() => ((EventSourceId)_result).Value.ShouldEqual(_guid.ToString());
}
