// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the information about an observer.
/// </summary>
/// <param name="Id">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer is observing.</param>
/// <param name="Type">The type of observer.</param>
/// <param name="Owner">The owner of the observer.</param>
/// <param name="EventTypes">The types of events the observer is observing.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer will observe.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled.</param>
/// <param name="RunningState">The running state of the observer.</param>
/// <param name="IsSubscribed">A value indicating whether the observer is subscribed to its handler.</param>
/// <param name="IsReplayable">A value indicating whether the observer supports replay scenarios.</param>
[ReadModel]
public record ObserverInformation(
    string Id,
    string EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    IEnumerable<EventType> EventTypes,
    ulong NextEventSequenceNumber,
    ulong LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    bool IsSubscribed,
    bool IsReplayable = true)
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <param name="observers">The observers service to query for observers.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    public static async Task<IEnumerable<ObserverInformation>> GetObservers(
        string eventStore,
        string @namespace,
        IObservers observers) =>
        (await observers.AllObservers(new() { EventStore = eventStore, Namespace = @namespace })).ToApi();

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <param name="observers">The observers service to observe for changes in observers.</param>
    /// <returns>An observable of a collection of <see cref="ObserverInformation"/>.</returns>
    public static ISubject<IEnumerable<ObserverInformation>> AllObservers(
        string eventStore,
        string @namespace,
        IObservers observers) =>
        observers.InvokeAndWrapWithTransformSubject(
            token => observers.ObserveObservers(new() { EventStore = eventStore, Namespace = @namespace }, token),
            observations => observations.ToApi());
}
