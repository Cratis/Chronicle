// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_redacting_an_event_with_sensitive_causation : given.an_event_sequence_storage
{
    const string Secret = "sensitive-command-property";
    Causation _redactionCausation;
    Causation _originalCausation;
    CorrelationId _originalCorrelation;
    DateTimeOffset _originalOccurred;
    EventEntry _stored;

    async Task Establish()
    {
        _redactionCausation = new Causation(DateTimeOffset.UtcNow, "redaction", new Dictionary<string, string> { ["actor"] = "operator" });
        _originalCausation = new Causation(DateTimeOffset.UtcNow, "command", new Dictionary<string, string> { ["apiKey"] = Secret });
        _originalCorrelation = CorrelationId.New();
        _originalOccurred = DateTimeOffset.UtcNow;
        await _storage.Append(
            1,
            EventSourceType.Default,
            EventSourceId.New(),
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            _originalCorrelation,
            [_originalCausation],
            [],
            [],
            _originalOccurred,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = new EventHash("original-payload-hash") });
    }

    async Task Because()
    {
        await _storage.Redact(1, "contains sensitive data", CorrelationId.New(), [_redactionCausation], [], DateTimeOffset.UtcNow);
        await using var context = CreateContext();
        _stored = await context.Events.SingleAsync(_ => _.SequenceNumber == 1UL);
    }

    [Fact] void should_not_retain_the_original_causation_in_content() => _stored.Content.ShouldNotContain(Secret);
    [Fact] void should_clear_the_original_content_hash() => _stored.ContentHashes.ShouldBeEmpty();
    [Fact] void should_not_retain_the_original_causation_in_the_event_field() => _stored.Causation.ShouldNotContain(Secret);
    [Fact] void should_keep_the_redaction_causation_in_the_event_field() => EventEntryConverter.GetCausation(_stored).Single().Properties["actor"].ShouldEqual("operator");
    [Fact] void should_keep_the_original_context_without_causation_properties_in_content()
    {
        using var document = JsonDocument.Parse(_stored.Content);
        var content = document.RootElement.GetProperty("1");
        content.GetProperty("originalEventType").GetString().ShouldEqual(_eventType.Id.Value);
        content.GetProperty("occurred").GetDateTimeOffset().ShouldEqual(_originalOccurred.AddTicks(-(_originalOccurred.Ticks % 10)));
        content.GetProperty("correlationId").GetString().ShouldEqual(_originalCorrelation.ToString());
        var cause = content.GetProperty("causation")[0];
        cause.GetProperty("type").GetString().ShouldEqual("command");
        cause.GetProperty("occurred").GetDateTimeOffset().ShouldEqual(_originalCausation.Occurred);
        cause.TryGetProperty("properties", out _).ShouldBeFalse();
    }
}
