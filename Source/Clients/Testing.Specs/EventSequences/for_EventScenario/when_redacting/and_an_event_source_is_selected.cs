// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_an_event_source_is_selected : given.a_scenario_with_notes
{
    IImmutableList<AppendedEvent> _events;

    async Task Because()
    {
        await _scenario.EventSequence.Redact(_redactedSource, "Erasure", typeof(RedactableNoteWritten));
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    AppendedEvent At(EventSequenceNumber number) => _events.Single(_ => _.Context.SequenceNumber == number);

    [Fact] void should_redact_the_first_note() => At(_firstNote).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_redact_the_second_note() => At(_secondNote).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_leave_the_tag_untouched() => At(_tag).Context.EventType.Id.ShouldEqual(_tagType);
    [Fact] void should_leave_the_other_source_untouched() => At(_otherNote).Context.EventType.Id.ShouldEqual(_noteType);
    [Fact] void should_keep_every_event_in_its_slot() => _events.Count.ShouldEqual(4);
}
