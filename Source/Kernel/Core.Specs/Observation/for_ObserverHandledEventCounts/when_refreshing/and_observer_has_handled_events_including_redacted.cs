// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverHandledEventCounts.when_refreshing;

public class and_observer_has_handled_events_including_redacted : given.all_dependencies
{
    const ulong LastHandledSequenceNumber = 9UL;
    const ulong TotalEventsIncludingRedacted = 10UL;

    ObserverDefinition _definition;
    ObserverState _state;

    void Establish()
    {
        _definition = new(
            "observer-with-redactions",
            [new EventType("event-type-1", 1)],
            EventSequenceId.Log,
            Concepts.Observation.ObserverType.Reactor,
            Concepts.Observation.ObserverOwner.Client,
            true);
        _state = new(
            "observer-with-redactions",
            LastHandledSequenceNumber,
            Concepts.Observation.ObserverRunningState.Active,
            new HashSet<Concepts.Keys.Key>(),
            new HashSet<Concepts.Keys.Key>(),
            [],
            0,
            false,
            false);

        _observerDefinitionsStorage.GetAll().Returns([_definition]);
        _observerStateStorage.GetAll().Returns([_state]);

        // Redacted events remain in the event sequence at their original sequence number with
        // their content rewritten to an EventRedacted event. Storage therefore returns the total
        // document count including redacted documents — the count semantic the grain preserves.
        _eventSequenceStorage.GetCount(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(new EventCount(TotalEventsIncludingRedacted));
    }

    async Task Because() => await _grain.Refresh();

    [Fact]
    async Task should_count_redacted_events_as_handled()
    {
        var counts = await _grain.GetAll();
        counts[new ObserverHandledEventCountKey(_definition.Identifier, _definition.EventSequenceId)]
            .ShouldEqual(new EventCount(TotalEventsIncludingRedacted));
    }
}
