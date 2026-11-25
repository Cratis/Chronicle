// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_nested_revision_events : Specification
{
    AppendedEvent[] _stored;
    AppendedEvent[] _public;
    string[] _serialized;

    void Establish()
    {
        _stored = given.revised_header_events.All().Select(@event => @event with
        {
            Content = JsonSerializer.Deserialize<ExpandoObject>(given.revised_header_events.Envelope(typeof(EventRevised).GetEventType(), JsonSerializer.Serialize(@event.Content)))!,
            OriginalContent = given.revised_header_events.Envelope(typeof(EventRevised).GetEventType(), @event.OriginalContent)
        }).ToArray();
    }

    void Because()
    {
        _public = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
        _serialized = _stored.Select(@event => PublicEventContentRedactor.Redact(@event.Context.EventType, @event.OriginalContent, given.header_events.Options)).ToArray();
    }

    [Fact] void should_redact_nested_current_content() => _public.All(@event => given.header_events.HasEmptyHeaders(JsonSerializer.Serialize(@event.Content))).ShouldBeTrue();
    [Fact] void should_redact_nested_serialized_content() => _serialized.All(given.header_events.HasEmptyHeaders).ShouldBeTrue();
    [Fact] void should_omit_nested_values() => given.header_events.ContainsSentinel(JsonSerializer.Serialize(_public)).ShouldBeFalse();
    [Fact] void should_preserve_nested_internal_values() => _stored.All(@event => given.header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
