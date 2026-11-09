// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_a_single_event_is_selected : given.a_scenario_with_notes
{
    IImmutableList<AppendedEvent> _events;
    JsonElement _redactedPayload;

    async Task Because()
    {
        await _scenario.EventSequence.Redact(_firstNote, "Subject asked for erasure");
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        _redactedPayload = JsonSerializer.Deserialize<JsonElement>(await _scenario.ReadContentAtRest(_firstNote));
    }

    AppendedEvent At(EventSequenceNumber number) => _events.Single(_ => _.Context.SequenceNumber == number);

    [Fact] void should_mark_the_selected_event_as_redacted() => At(_firstNote).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_keep_every_event_in_its_slot() => _events.Count.ShouldEqual(4);
    [Fact] void should_keep_the_event_source() => At(_firstNote).Context.EventSourceId.ShouldEqual(_redactedSource);
    [Fact] void should_record_the_reason() => _redactedPayload.GetProperty("reason").GetString().ShouldEqual("Subject asked for erasure");
    [Fact] void should_record_the_original_event_type() => _redactedPayload.GetProperty("originalEventType").GetString().ShouldEqual(_noteType.Value);
    [Fact] void should_leave_the_second_note_untouched() => ((RedactableNoteWritten)At(_secondNote).Content).Text.ShouldEqual("second");
    [Fact] void should_leave_the_tag_untouched() => At(_tag).Context.EventType.Id.ShouldEqual(_tagType);
    [Fact] void should_leave_the_other_source_untouched() => ((RedactableNoteWritten)At(_otherNote).Content).Text.ShouldEqual("other");
}
