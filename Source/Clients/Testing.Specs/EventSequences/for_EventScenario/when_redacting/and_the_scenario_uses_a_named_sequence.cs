// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_the_scenario_uses_a_named_sequence : Specification
{
    EventScenario _scenario;
    EventSequenceNumber _sequenceNumber;
    IImmutableList<AppendedEvent> _events;
    JsonElement _payload;

    async Task Establish()
    {
        _scenario = new EventScenario(new EventSequenceId("redaction-audit"), "test-event-store", "default");
        _sequenceNumber = (await _scenario.EventSequence.Append(EventSourceId.New(), new RedactableNoteWritten("named"))).SequenceNumber;
    }

    async Task Because()
    {
        await _scenario.EventSequence.Redact(_sequenceNumber, "Named sequence");
        _events = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
        _payload = JsonSerializer.Deserialize<JsonElement>(await _scenario.ReadContentAtRest(_sequenceNumber));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_mark_the_event_as_redacted() => _events.Single().Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_record_the_reason() => _payload.GetProperty("reason").GetString().ShouldEqual("Named sequence");
}
