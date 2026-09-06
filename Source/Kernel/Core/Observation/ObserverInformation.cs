// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the read model for observer information, providing query access to the observer state and definition stores.
/// </summary>
/// <param name="Id">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer is observing.</param>
/// <param name="Type">The type of observer.</param>
/// <param name="Owner">The owner of the observer.</param>
/// <param name="EventTypes">The identifiers of event types the observer is observing.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer will observe.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled.</param>
/// <param name="TailEventSequenceNumber">The tail event sequence number of the event sequence.</param>
/// <param name="HandledEventCount">The number of events the observer has handled.</param>
/// <param name="RunningState">The running state of the observer.</param>
/// <param name="IsSubscribed">Whether the observer is subscribed to its handler.</param>
/// <param name="IsReplayable">Whether the observer supports replay scenarios.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Observers)]
public record ObserverInformation(
    string Id,
    string EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    IEnumerable<Contracts.Events.EventType> EventTypes,
    ulong NextEventSequenceNumber,
    ulong LastHandledEventSequenceNumber,
    ulong TailEventSequenceNumber,
    ulong HandledEventCount,
    ObserverRunningState RunningState,
    bool IsSubscribed,
    bool IsReplayable)
{
    /// <summary>
    /// Gets all observers for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read observers from.</param>
    /// <returns>A collection of observer information.</returns>
    internal static async Task<IEnumerable<ObserverInformation>> AllObservers(EventStoreName eventStore, EventStoreNamespaceName @namespace, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).Observers.GetAll();
        var states = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.GetAll();
        return ObserverInformationConverters.Join(definitions, states);
    }

    /// <summary>
    /// Gets the replayable observers in a namespace that consume any of a set of event types.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="eventTypes">The identifiers of the event types to find observers for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read observers from.</param>
    /// <returns>A collection of observer information.</returns>
    /// <remarks>
    /// The join is an inner one on purpose: an observer that has never run has nothing to replay, so unlike the
    /// all-observers listing it must not appear here.
    /// </remarks>
    internal static async Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IEnumerable<string> eventTypes,
        IStorage storage)
    {
        var types = eventTypes.Select(eventType => new Concepts.Events.EventType(eventType, Concepts.Events.EventTypeGeneration.First, false));
        var definitions = await storage.GetEventStore(eventStore).Observers.GetReplayableObserversForEventTypes(types);
        var states = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.GetAll();

        return from definition in definitions
               join state in states on definition.Identifier equals state.Identifier
               select ObserverInformationConverters.ToObserverInformation(definition, state);
    }

    /// <summary>
    /// Observes all observers for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe observers from.</param>
    /// <returns>An observable subject emitting collections of observer information.</returns>
    internal static ISubject<IEnumerable<ObserverInformation>> ObserveObservers(EventStoreName eventStore, EventStoreNamespaceName @namespace, IStorage storage)
    {
        var subject = new ReplaySubject<IEnumerable<ObserverInformation>>(1);
        storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).Observers
            .ObserveAll()
            .SelectMany(async states =>
            {
                var definitions = await storage.GetEventStore(eventStore).Observers.GetAll();
                return ObserverInformationConverters.Join(definitions, states);
            })
            .Subscribe(subject.OnNext);
        return subject;
    }
}
