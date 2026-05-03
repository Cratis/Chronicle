// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_EventSourceIdJsonConverter.when_reading;

public class with_a_string_value : Specification
{
    const string Input = "some-id";
    JsonSerializerOptions _options;
    EventSourceId<string> _result;

    void Establish() => _options = new JsonSerializerOptions { Converters = { new EventSourceIdJsonConverterFactory() } };

    void Because() => _result = JsonSerializer.Deserialize<EventSourceId<string>>($"\"{Input}\"", _options)!;

    [Fact] void should_have_the_string_as_typed_value() => _result.TypedValue.ShouldEqual(Input);
    [Fact] void should_have_the_string_as_event_source_id_value() => ((EventSourceId)_result).Value.ShouldEqual(Input);
}
