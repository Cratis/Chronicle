// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_EventSourceIdJsonConverter.when_writing;

public class with_a_concept_wrapping_a_string : Specification
{
    const string Input = "some-concept-id";
    JsonSerializerOptions _options;
    string _result;

    void Establish() => _options = new JsonSerializerOptions { Converters = { new EventSourceIdJsonConverterFactory() } };

    void Because() => _result = JsonSerializer.Serialize(new EventSourceId<StringConcept>(new StringConcept(Input)), _options);

    [Fact] void should_serialize_as_the_plain_string() => _result.ShouldEqual($"\"{Input}\"");
}
