// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_redacting_an_event_with_sensitive_causation : given.a_storage_with_appended_events
{
    const string Secret = "sensitive-command-property";
    Causation _redactionCausation;
    AppendedEvent _stored;

    async Task Establish()
    {
        _redactionCausation = new Causation(DateTimeOffset.UtcNow, "redaction", new Dictionary<string, string> { ["actor"] = "operator" });
        await _storage.Append(
            3,
            EventSourceType.Default,
            _secondEventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, "command", new Dictionary<string, string> { ["apiKey"] = Secret })],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
    }

    async Task Because()
    {
        await _storage.Redact(3, "contains sensitive data", CorrelationId.New(), [_redactionCausation], [], DateTimeOffset.UtcNow);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)3UL);
    }

    [Fact] void should_remove_the_original_causation_from_the_replacement_content() => ((IEnumerable<Causation>)((IDictionary<string, object?>)_stored.Content)["causation"]!).ShouldBeEmpty();
    [Fact] void should_replace_the_stored_causation_with_the_redaction_causation() => _stored.Context.Causation.ShouldEqual([_redactionCausation]);
    [Fact] void should_not_retain_the_sensitive_command_property_in_the_stored_event() => System.Text.Json.JsonSerializer.Serialize(_stored).ShouldNotContain(Secret);
}
