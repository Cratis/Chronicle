// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_the_released_json_format : given.a_normalized_event
{
    AppendedEvent _result;

    void Because() => _result = serializer.Deserialize(Convert.FromBase64String(given.LegacyWireFixtures.Single))!;

    [Fact] void should_preserve_the_envelope() => JsonSerializer.Serialize(_result, options).ShouldEqual(JsonSerializer.Serialize(appended_event, options));

    [Fact] void should_preserve_nested_runtime_types() => ContentTypes(_result.Content).ShouldEqual(ContentTypes(appended_event.Content));

    [Fact] void should_preserve_context() => JsonSerializer.Serialize(_result.Context, options).ShouldEqual(JsonSerializer.Serialize(appended_event.Context, options));

    [Fact] void should_preserve_projected_content() => JsonNode.DeepEquals(converter.ToJsonObject(_result.Content, schema), converter.ToJsonObject(appended_event.Content, schema)).ShouldBeTrue();

    [Fact] void should_preserve_runtime_types() => _result.Content.Select(property => property.Value?.GetType()).ShouldEqual(appended_event.Content.Select(property => property.Value?.GetType()));
}
