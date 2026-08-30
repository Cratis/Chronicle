// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an observer that consumes a given event type, together with the namespace it runs in.
/// </summary>
/// <param name="Id">The identity of the pairing, which is the observer within its namespace.</param>
/// <param name="Namespace">The namespace the observer runs in.</param>
/// <param name="Observer">The observer.</param>
/// <remarks>
/// Observers are defined once per event store but run per namespace, so "who consumes this event type" only has an
/// answer once the namespace is carried alongside.
/// </remarks>
[ReadModel]
public record ObserverInformationForEventType(string Id, string Namespace, ObserverInformation Observer)
{
    /// <summary>
    /// Gets every observer consuming an event type, across all namespaces in an event store.
    /// </summary>
    /// <param name="eventStore">The event store to search.</param>
    /// <param name="eventTypeId">The identifier of the event type to find consuming observers for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read observers and namespaces from.</param>
    /// <returns>A collection of <see cref="ObserverInformationForEventType"/>.</returns>
    internal static async Task<IEnumerable<ObserverInformationForEventType>> GetObserversForEventType(
        EventStoreName eventStore,
        string eventTypeId,
        IStorage storage)
    {
        var namespaces = (await storage.GetEventStore(eventStore).Namespaces.GetAll()).Select(_ => _.Name.Value);
        var results = new List<ObserverInformationForEventType>();

        foreach (var @namespace in namespaces)
        {
            var observers = await ObserverInformation.AllObservers(eventStore, @namespace, storage);
            results.AddRange(ObserverInformationForEventTypeFilter.FilterByEventType(@namespace, observers, eventTypeId));
        }

        return results;
    }

    /// <summary>
    /// Observes every observer consuming an event type, across all namespaces in an event store.
    /// </summary>
    /// <param name="eventStore">The event store to search.</param>
    /// <param name="eventTypeId">The identifier of the event type to find consuming observers for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read observers and namespaces from.</param>
    /// <returns>An observable subject emitting collections of <see cref="ObserverInformationForEventType"/>.</returns>
    internal static ISubject<IEnumerable<ObserverInformationForEventType>> ObserveObserversForEventType(
        EventStoreName eventStore,
        string eventTypeId,
        IStorage storage)
    {
        var subject = new ReplaySubject<IEnumerable<ObserverInformationForEventType>>(1);
        storage
            .GetEventStore(eventStore)
            .Namespaces
            .ObserveAll()
            .Select(namespaces => ObserverInformationForEventTypeFilter.ObserveForAllNamespaces(eventStore, eventTypeId, namespaces.Select(_ => _.Name.Value), storage))
            .Switch()
            .Subscribe(subject.OnNext);
        return subject;
    }
}
