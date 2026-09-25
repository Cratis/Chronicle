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
    EventEntry _stored;

    async Task Establish()
    {
        _redactionCausation = new Causation(DateTimeOffset.UtcNow, "redaction", new Dictionary<string, string> { ["actor"] = "operator" });
        await _storage.Append(
            1,
            EventSourceType.Default,
            EventSourceId.New(),
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
        await _storage.Redact(1, "contains sensitive data", CorrelationId.New(), [_redactionCausation], [], DateTimeOffset.UtcNow);
        await using var context = CreateContext();
        _stored = await context.Events.SingleAsync(_ => _.SequenceNumber == 1UL);
    }

    [Fact] void should_not_retain_the_original_causation_in_content() => _stored.Content.ShouldNotContain(Secret);
    [Fact] void should_not_retain_the_original_causation_in_the_event_field() => _stored.Causation.ShouldNotContain(Secret);
    [Fact] void should_keep_the_redaction_causation_in_the_event_field() => EventEntryConverter.GetCausation(_stored).Single().Properties["actor"].ShouldEqual("operator");
    [Fact] void should_keep_the_redaction_causation_type_in_content()
    {
        using var document = JsonDocument.Parse(_stored.Content);
        document.RootElement.GetProperty("1").GetProperty("causation")[0].GetProperty("type").GetString().ShouldEqual("redaction");
    }
}
