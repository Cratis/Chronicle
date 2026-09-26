// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_redacting_an_event_with_sensitive_causation : given.a_storage_with_appended_events
{
    const string Secret = "sensitive-command-property";
    Causation _redactionCausation;
    Causation _originalCausation;
    CorrelationId _originalCorrelation;
    DateTimeOffset _originalOccurred;
    IdentityId _originalIdentity;
    AppendedEvent _stored;

    async Task Establish()
    {
        _redactionCausation = new Causation(DateTimeOffset.UtcNow, "redaction", new Dictionary<string, string> { ["actor"] = "operator" });
        _originalCausation = new Causation(DateTimeOffset.UtcNow, "command", new Dictionary<string, string> { ["apiKey"] = Secret });
        _originalCorrelation = CorrelationId.New();
        _originalOccurred = DateTimeOffset.UtcNow;
        _originalIdentity = IdentityId.New();
        await _storage.Append(
            3,
            EventSourceType.Default,
            _secondEventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            _originalCorrelation,
            [_originalCausation],
            [_originalIdentity],
            [],
            _originalOccurred,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());
    }

    async Task Because()
    {
        await _storage.Redact(3, "contains sensitive data", CorrelationId.New(), [_redactionCausation], [], DateTimeOffset.UtcNow);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)3UL);
    }

    [Fact] void should_keep_only_the_original_causation_type_and_time()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(_stored.Content));
        var cause = document.RootElement.GetProperty("causation")[0];
        cause.GetProperty("type").GetString().ShouldEqual(_originalCausation.Type.Value);
        cause.GetProperty("occurred").GetDateTimeOffset().ShouldEqual(_originalCausation.Occurred);
        cause.TryGetProperty("properties", out _).ShouldBeFalse();
    }
    [Fact] void should_keep_the_original_type() => ((IDictionary<string, object?>)_stored.Content)["originalEventType"].ShouldEqual(_eventType.Id.Value);
    [Fact] void should_keep_the_original_occurrence() => ((IDictionary<string, object?>)_stored.Content)["occurred"].ShouldEqual(_originalOccurred);
    [Fact] void should_keep_the_original_correlation() => ((IDictionary<string, object?>)_stored.Content)["correlationId"].ShouldEqual(_originalCorrelation.Value);
    [Fact] void should_keep_the_original_caused_by_identity() => ((IEnumerable<string>)((IDictionary<string, object?>)_stored.Content)["causedBy"]!).Single().ShouldEqual(_originalIdentity.ToString());
    [Fact] void should_replace_the_stored_causation_with_the_redaction_causation() => _stored.Context.Causation.ShouldEqual([_redactionCausation]);
    [Fact] void should_not_retain_the_sensitive_command_property_in_the_stored_event() => System.Text.Json.JsonSerializer.Serialize(_stored).ShouldNotContain(Secret);
}
