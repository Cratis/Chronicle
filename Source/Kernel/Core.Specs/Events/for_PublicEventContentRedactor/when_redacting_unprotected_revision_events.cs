// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.Events.for_PublicEventContentRedactor;

public class when_redacting_unprotected_revision_events : Specification
{
    string[] _envelopes;
    string[] _serialized;
    AppendedEvent[] _stored;
    AppendedEvent[] _public;

    void Establish()
    {
        _envelopes = new[] { "ApplicationEvent", "webhookadded", "targetheaderssetforwebhook" }
            .SelectMany(id => new[] { "{\"targetHeaders\":{\"Token\":\"current-header-value\"}}", "current-header-value", string.Empty }
                .Select(content => given.revised_header_events.Envelope(new EventType(id, 1), content))).ToArray();
        _stored = _envelopes.Select(content => given.revised_header_events.All()[0] with
        {
            Content = JsonSerializer.Deserialize<ExpandoObject>(content)!,
            OriginalContent = content,
            Revisions = [],
            GenerationalContent = new Dictionary<int, string> { [1] = content }
        }).ToArray();
    }

    void Because()
    {
        _serialized = _envelopes.Select(content => PublicEventContentRedactor.Redact(typeof(EventRevised).GetEventType(), content, given.header_events.Options)).ToArray();
        _public = _stored.Select(@event => PublicEventContentRedactor.Redact(@event, given.header_events.Options)).ToArray();
    }

    [Fact] void should_pass_through_non_protected_serialized_content() => _serialized.ShouldEqual(_envelopes);
    [Fact] void should_pass_through_non_protected_current_content() => _public.Select(@event => JsonSerializer.Serialize(@event.Content)).ShouldEqual(_stored.Select(@event => JsonSerializer.Serialize(@event.Content)));
    [Fact] void should_pass_through_non_protected_histories() => _public.Select(@event => @event.OriginalContent).ShouldEqual(_envelopes);
    [Fact] void should_pass_through_non_protected_generations() => _public.Select(@event => @event.GenerationalContent[1]).ShouldEqual(_envelopes);
}
