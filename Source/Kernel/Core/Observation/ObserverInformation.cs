// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

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
    IEnumerable<string> EventTypes,
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
    internal static async Task<IEnumerable<ObserverInformation>> AllObservers(string eventStore, string @namespace, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).Observers.GetAll();
        var states = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.GetAll();
        return Join(definitions, states);
    }

    /// <summary>
    /// Observes all observers for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The namespace within the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe observers from.</param>
    /// <returns>An observable subject emitting collections of observer information.</returns>
    internal static ISubject<IEnumerable<ObserverInformation>> ObserveObservers(string eventStore, string @namespace, IStorage storage)
    {
        var subject = new ReplaySubject<IEnumerable<ObserverInformation>>(1);
        storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace).Observers
            .ObserveAll()
            .SelectMany(async states =>
            {
                var definitions = await storage.GetEventStore(eventStore).Observers.GetAll();
                return Join(definitions, states);
            })
            .Subscribe(subject.OnNext);
        return subject;
    }

    /// <summary>
    /// Joins observer definitions with their state.
    /// </summary>
    /// <param name="definitions">The observer definitions.</param>
    /// <param name="states">The observer states.</param>
    /// <returns>The joined observer information.</returns>
    /// <remarks>
    /// Left outer join on purpose: an observer that has been defined but has not run yet has no state,
    /// and it still belongs in the listing. An inner join would hide it until it first handled an event.
    /// </remarks>
    private static IEnumerable<ObserverInformation> Join(
        IEnumerable<ObserverDefinition> definitions,
        IEnumerable<ObserverState> states) =>
        from definition in definitions
        join state in states on definition.Identifier equals state.Identifier into stateGroup
        from state in stateGroup.DefaultIfEmpty(ObserverState.Empty)
        select ToObserverInformation(definition, state);

    private static ObserverInformation ToObserverInformation(ObserverDefinition definition, ObserverState state) =>
        new(
            definition.Identifier,
            definition.EventSequenceId,
            (ObserverType)(int)definition.Type,
            (ObserverOwner)(int)definition.Owner,
            definition.EventTypes.Select(et => et.Id.Value),
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber,
            state.TailEventSequenceNumber,
            state.HandledEventCount,
            (ObserverRunningState)(int)state.RunningState,

            // Subscription state is per-observer and only known by the observer grain, so the listing
            // reports false rather than activating every grain to ask. Read a single observer to get it.
            IsSubscribed: false,
            definition.IsReplayable);
}
