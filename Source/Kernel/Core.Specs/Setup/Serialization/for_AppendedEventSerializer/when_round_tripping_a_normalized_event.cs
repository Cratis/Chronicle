// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_round_tripping_a_normalized_event : given.a_normalized_event
{
    AppendedEvent _result;
    IDictionary<string, object?> _original;
    IDictionary<string, object?> _content;

    void Establish() => _original = appended_event.Content;

    void Because()
    {
        _result = serializer.Deserialize(serializer.SerializeToArray(appended_event))!;
        _content = _result.Content;
    }

    [Fact] void should_preserve_context() => JsonSerializer.Serialize(_result.Context, options).ShouldEqual(JsonSerializer.Serialize(appended_event.Context, options));

    [Fact] void should_preserve_projected_content() => JsonNode.DeepEquals(converter.ToJsonObject(_result.Content, schema), converter.ToJsonObject(appended_event.Content, schema)).ShouldBeTrue();

    [Fact] void should_preserve_runtime_types() => _content.Values.Select(value => value?.GetType()).ShouldEqual(_original.Values.Select(value => value?.GetType()));

    [Fact] void should_preserve_property_order() => _content.Keys.ShouldEqual(_original.Keys);

    [Fact] void should_preserve_decimal_bits() => decimal.GetBits((decimal)_content["decimal"]!).ShouldEqual(decimal.GetBits((decimal)_original["decimal"]!));

    [Fact] void should_preserve_date_time_kind() => ((DateTime)_content["datetime"]!).Kind.ShouldEqual(((DateTime)_original["datetime"]!).Kind);

    [Fact] void should_preserve_offset() => ((DateTimeOffset)_content["timestamp"]!).Offset.ShouldEqual(((DateTimeOffset)_original["timestamp"]!).Offset);
}
