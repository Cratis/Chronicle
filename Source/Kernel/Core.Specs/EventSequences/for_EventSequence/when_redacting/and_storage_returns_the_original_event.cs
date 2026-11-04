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

public class and_storage_returns_the_original_event : given.an_event_sequence
{
    static readonly EventSourceId _returnedEventSource = "returned-event-source";

    IObserverDefinitionsStorage _observerDefinitions;
    IObserver _observer;
    IEnumerable<EventType> _lookedUpEventTypes;

    void Establish()
    {
        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        _observerDefinitions.GetReplayableObserversForEventTypes(Arg.Do<IEnumerable<EventType>>(types => _lookedUpEventTypes = types.ToArray()))
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
                    _eventType,
                    EventSourceType.Default,
                    _returnedEventSource,
                    EventStreamType.All,
                    EventStreamId.Default,
                    0UL,
                    CorrelationId.New()),
                new ExpandoObject()));
    }

    async Task Because() => await _eventSequence.Redact(0UL, RedactionReason.Unknown, CorrelationId.New(), [], Identity.System);

    [Fact] void should_look_up_observers_for_exactly_the_original_event_type() => _lookedUpEventTypes.ShouldEqual([_eventType]);
    [Fact] void should_replay_the_returned_event_source_partition_once() => _observer.Received(1).ReplayPartition((Key)_returnedEventSource);
    [Fact] void should_not_replay_any_other_partition() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.ReplayPartition)).ShouldEqual(1);
}
