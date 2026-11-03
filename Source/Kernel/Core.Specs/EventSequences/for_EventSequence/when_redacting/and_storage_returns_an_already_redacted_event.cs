// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_redacting;

public class and_storage_returns_an_already_redacted_event : given.an_event_sequence
{
    IObserverDefinitionsStorage _observerDefinitions;
    IObserver _observer;

    void Establish()
    {
        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        _observerDefinitions.GetReplayableObserversForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IEnumerable<ObserverDefinition>>(
            [
                new ObserverDefinition("replayable-observer", [_eventType], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)
            ]));
        _eventStoreStorage.Observers.Returns(_observerDefinitions);

        _observer = Substitute.For<IObserver>();
        _silo.AddProbe<IObserver>(_ => _observer);

        _eventSequenceStorage.Redact(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<RedactionReason>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<DateTimeOffset>())
            .Returns(new AppendedEvent(
                EventContext.From(
                    EventStore,
                    EventStoreNamespace,
                    new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First),
                    EventSourceType.Default,
                    _eventSourceId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    0UL,
                    CorrelationId.New()),
                new ExpandoObject()));
    }

    async Task Because() => await _eventSequence.Redact(0UL, RedactionReason.Unknown, CorrelationId.New(), [], Identity.System);

    [Fact]
    void should_redact_in_storage_once() => _eventSequenceStorage.Received(1).Redact(
        Arg.Any<EventSequenceNumber>(),
        Arg.Any<RedactionReason>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<IEnumerable<IdentityId>>(),
        Arg.Any<DateTimeOffset>());
    [Fact] void should_not_look_up_observers_by_event_type() => _observerDefinitions.DidNotReceiveWithAnyArgs().GetReplayableObserversForEventTypes(default!);
    [Fact] void should_not_replay_any_partition() => _observer.DidNotReceive().ReplayPartition(Arg.Any<Key>());
}
