// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_events_have_individual_causation : given.appending_many_events
{
    Causation _ambient;
    Causation _first;
    Causation _second;
    EventToAppendToStorage[] _storedEvents;

    void Establish()
    {
        _ambient = new(DateTimeOffset.UnixEpoch, "ambient", new Dictionary<string, string>());
        _first = new(DateTimeOffset.UnixEpoch, "first", new Dictionary<string, string>());
        _second = new(DateTimeOffset.UnixEpoch, "second", new Dictionary<string, string>());
        _events =
        [
            EventToAppendFor("first") with { Causation = [_ambient, _first] },
            EventToAppendFor("second") with { Causation = [_ambient, _second] },
            EventToAppendFor("third")
        ];
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _storedEvents = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(_storedEvents)));
            });
    }

    async Task Because() => await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [_ambient],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_persist_the_first_events_causation() => _storedEvents[0].Causation.ShouldEqual([_ambient, _first]);
    [Fact] void should_persist_the_second_events_causation() => _storedEvents[1].Causation.ShouldEqual([_ambient, _second]);
    [Fact] void should_persist_the_ambient_chain_for_an_event_without_override() => _storedEvents[2].Causation.ShouldEqual([_ambient]);
}
