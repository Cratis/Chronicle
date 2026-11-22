// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_serialized_header_shapes : Specification
{
    string[] _inputs;
    string[] _results;

    void Establish() => _inputs =
    [
        """{"targetHeaders":{"Same":"current-header-value","Same":"original-header-value"},"targetHeaders":{"Same":"generation-one-header-value","Same":"generation-three-header-value"},"TargetHeaders":{"Other":"revision-one-header-value"},"TARGETHEADERS":{"Mixed":"revision-two-header-value"},"name":"unchanged"}""",
        """{"targetHeaders":null,"name":"unchanged"}""",
        """{"targetHeaders":"current-header-value","name":"unchanged"}""",
        """{"targetHeaders":["current-header-value"],"name":"unchanged"}""",
        """{"targetHeaders":{"Nested":{"value":"current-header-value"},"Null":null,"Number":12},"name":"unchanged"}""",
        """{"targetHeaders":{},"name":"unchanged"}""",
        """{"name":"unchanged"}"""
    ];

    void Because() => _results = _inputs.Select(content => PublicEventContentRedactor.Redact(new EventType("TargetHeadersSetForWebhook", 3), content, given.header_events.Options)).ToArray();

    [Fact] void should_omit_values_in_every_shape() => _results.All(content => !given.header_events.ContainsSentinel(content)).ShouldBeTrue();
    [Fact] void should_keep_unrelated_fields() => _results.All(content => JsonElement.Parse(content).GetProperty("name").GetString() == "unchanged").ShouldBeTrue();
    [Fact] void should_keep_duplicate_header_names_with_empty_values() => JsonElement.Parse(_results[0]).GetProperty("targetHeaders").EnumerateObject().Select(property => property.Value.GetString()).ShouldEqual([string.Empty, string.Empty]);
    [Fact] void should_keep_every_duplicate_header_map() => JsonElement.Parse(_results[0]).EnumerateObject().Count(property => property.Name == "targetHeaders").ShouldEqual(2);
    [Fact] void should_redact_every_casing() => JsonElement.Parse(_results[0]).EnumerateObject().Where(property => property.Name != "name").All(property => property.Value.EnumerateObject().All(header => header.Value.GetString() is { Length: 0 })).ShouldBeTrue();
    [Fact] void should_replace_non_object_headers_with_empty_maps() => _results.Skip(1).Take(3).All(content => !JsonElement.Parse(content).GetProperty("targetHeaders").EnumerateObject().Any()).ShouldBeTrue();
    [Fact] void should_not_add_absent_headers() => JsonElement.Parse(_results[^1]).TryGetProperty("targetHeaders", out _).ShouldBeFalse();
}
