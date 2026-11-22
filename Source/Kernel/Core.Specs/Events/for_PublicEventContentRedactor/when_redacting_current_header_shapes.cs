// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_current_header_shapes : Specification
{
    AppendedEvent[] _stored;
    AppendedEvent[] _once;
    AppendedEvent[] _twice;

    void Establish()
    {
        var cyclicHeaders = new Dictionary<string, object?>();
        cyclicHeaders["Nested"] = cyclicHeaders;
        object?[] shapes = [cyclicHeaders, new Dictionary<string, string>(), null, "current-header-value", new[] { "current-header-value" }, JsonElement.Parse("""{"Same":"current-header-value","Same":"original-header-value"}"""), new Dictionary<string, object?> { ["Nested"] = new { value = "current-header-value" } }];
        _stored = shapes.Select(shape =>
        {
            IDictionary<string, object?> content = new ExpandoObject();
            content["targetHeaders"] = shape;
            content["TargetHeaders"] = shape;
            content["TARGETHEADERS"] = shape;
            content["name"] = "unchanged";
            return given.header_events.All()[0] with { Content = (ExpandoObject)content };
        }).Append(given.header_events.All()[0] with { Content = new ExpandoObject() }).ToArray();
    }

    void Because()
    {
        _once = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
        _twice = _once.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
    }

    [Fact] void should_redact_every_shape_and_casing() => _once.All(@event => !given.header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_preserve_unrelated_fields() => _once[..^1].All(@event => ((IDictionary<string, object?>)@event.Content)["name"]!.Equals("unchanged")).ShouldBeTrue();
    [Fact] void should_not_add_absent_headers() => _once[^1].Content.ShouldBeEmpty();
    [Fact] void should_be_idempotent() => JsonSerializer.Serialize(_twice).ShouldEqual(JsonSerializer.Serialize(_once));
    [Fact] void should_not_change_original_histories() => _stored.All(@event => given.header_events.ContainsSentinel(@event.OriginalContent)).ShouldBeTrue();
}
