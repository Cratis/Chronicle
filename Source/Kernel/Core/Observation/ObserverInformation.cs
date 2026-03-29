// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            .Subscribe(states =>
            {
                var definitions = storage.GetEventStore(eventStore).Observers.GetAll().GetAwaiter().GetResult();
                subject.OnNext(Join(definitions, states));
            });
        return subject;
    }

    private static IEnumerable<ObserverInformation> Join(
        IEnumerable<ObserverDefinition> definitions,
        IEnumerable<ObserverState> states) =>
        from definition in definitions
        join state in states on definition.Identifier equals state.Identifier
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
            (ObserverRunningState)(int)state.RunningState,
            false,
            definition.IsReplayable);
}
