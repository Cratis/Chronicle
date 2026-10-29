// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_another_scenario_holds_the_same_sequence_number : Specification
{
    EventScenario _mine;
    EventScenario _theirs;
    IImmutableList<AppendedEvent> _myEvents;
    IImmutableList<AppendedEvent> _theirEvents;

    async Task Establish()
    {
        _mine = new EventScenario();
        _theirs = new EventScenario();
        await _mine.EventLog.Append(EventSourceId.New(), new RedactableNoteWritten("mine"));
        await _theirs.EventLog.Append(EventSourceId.New(), new RedactableNoteWritten("theirs"));
    }

    async Task Because()
    {
        await _mine.EventSequence.Redact(EventSequenceNumber.First, "Mine only");
        _myEvents = await _mine.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        _theirEvents = await _theirs.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    void Destroy()
    {
        _mine.Dispose();
        _theirs.Dispose();
    }

    [Fact] void should_redact_the_event_in_the_first_scenario() => _myEvents.Single().Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_leave_the_other_scenario_untouched() => ((RedactableNoteWritten)_theirEvents.Single().Content).Text.ShouldEqual("theirs");
}
