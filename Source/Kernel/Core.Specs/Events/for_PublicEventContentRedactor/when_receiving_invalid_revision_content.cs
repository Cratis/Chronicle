// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_receiving_invalid_revision_content : Specification
{
    string[] _malformed;
    Exception?[] _serializedErrors;
    Exception?[] _currentErrors;

    void Establish()
    {
        var types = given.header_events.Direct().Select(@event => @event.Context.EventType);
        object?[] contents = [null, 42, new { targetHeaders = new { Token = "current-header-value" } }, string.Empty, "current-header-value", "{\"targetHeaders\":\"current-header-value\"", "null", "[]"];
        _malformed = [.. types.SelectMany(type => contents.Select(content => JsonSerializer.Serialize(new
        {
            eventType = new { id = type.Id.Value, generation = type.Generation.Value },
            content
        }))),
            """{"eventType":{"id":"WebhookAdded","generation":1}}""",
            """{"eventType":{"id":"WebhookAdded","generation":1},"content":"{}","Content":"current-header-value"}""",
            """{"eventType":{"id":"WebhookAdded","Id":"ApplicationEvent"},"content":"current-header-value"}""",
            """{"eventType":{"id":"ApplicationEvent"},"EventType":{"id":"WebhookAdded"},"content":"current-header-value"}""",
            """{"eventType":"WebhookAdded","content":"current-header-value"}""",
            """{"eventType":{"id":null},"content":"current-header-value"}""",
            """{"content":"current-header-value"}"""];
    }

    void Because()
    {
        _serializedErrors = _malformed.Concat(["current-header-value", "null", "[]"]).Select(content => Catch.Exception(() => PublicEventContentRedactor.Redact(typeof(EventRevised).GetEventType(), content, given.header_events.Options))).ToArray();
        _currentErrors = _malformed.Select(content => Catch.Exception(() => PublicEventContentRedactor.Redact(given.revised_header_events.All()[0] with { Content = JsonSerializer.Deserialize<ExpandoObject>(content)! }, given.header_events.Options))).ToArray();
    }

    [Fact] void should_reject_malformed_serialized_envelopes() => _serializedErrors.All(error => error is JsonException { Message: "Invalid public event content.", InnerException: null }).ShouldBeTrue();
    [Fact] void should_reject_malformed_current_envelopes() => _currentErrors.All(error => error is JsonException { Message: "Invalid public event content.", InnerException: null }).ShouldBeTrue();
    [Fact] void should_not_include_values_in_errors() => given.header_events.ContainsSentinel(string.Join(" ", _serializedErrors.Concat(_currentErrors).Select(error => error?.ToString()))).ShouldBeFalse();
}
