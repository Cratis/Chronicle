// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_EventSourceIdJsonConverter.when_writing;

public class with_a_guid_type : Specification
{
    static readonly Guid _guid = Guid.Parse("b4e2b6a0-1b3e-4e6a-8c4d-2f7e9b1a3c5d");
    JsonSerializerOptions _options;
    string _result;

    void Establish() => _options = new JsonSerializerOptions { Converters = { new EventSourceIdJsonConverterFactory() } };

    void Because() => _result = JsonSerializer.Serialize(new EventSourceId<Guid>(_guid), _options);

    [Fact] void should_serialize_as_the_guid_string() => _result.ShouldEqual($"\"{_guid}\"");
}
