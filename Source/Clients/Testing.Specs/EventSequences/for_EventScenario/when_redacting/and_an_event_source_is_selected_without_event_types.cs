// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting;

public class and_an_event_source_is_selected_without_event_types : given.a_scenario_with_notes
{
    IImmutableList<AppendedEvent> _events;

    async Task Because()
    {
        await _scenario.EventSequence.Redact(_redactedSource, "Erasure");
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    AppendedEvent At(EventSequenceNumber number) => _events.Single(_ => _.Context.SequenceNumber == number);

    [Fact] void should_redact_the_first_note() => At(_firstNote).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_redact_the_tag() => At(_tag).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_redact_the_second_note() => At(_secondNote).Context.EventType.Id.Value.ShouldEqual("EventRedacted");
    [Fact] void should_leave_the_other_source_untouched() => At(_otherNote).Context.EventType.Id.ShouldEqual(_noteType);
}
