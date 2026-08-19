// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Narrows a namespace's observers down to the ones consuming a given event type.
/// </summary>
internal static class ObserverInformationForEventTypeFilter
{
    /// <summary>
    /// Filters the observers in a namespace by event type identifier.
    /// </summary>
    /// <param name="namespace">The namespace the observers belong to.</param>
    /// <param name="observersInNamespace">The observers running in that namespace.</param>
    /// <param name="eventTypeId">The identifier of the event type to filter by.</param>
    /// <returns>The observers consuming the event type, each paired with the namespace.</returns>
    /// <remarks>
    /// The generation is deliberately not part of the match - an observer consuming any generation of the type is a
    /// consumer of it.
    /// </remarks>
    internal static IEnumerable<ObserverInformationForEventType> FilterByEventType(
        string @namespace,
        IEnumerable<ObserverInformation> observersInNamespace,
        string eventTypeId) =>
        observersInNamespace
            .Where(observer => observer.EventTypes.Any(eventType => string.Equals(eventType.Id, eventTypeId, StringComparison.Ordinal)))
            .Select(observer => new ObserverInformationForEventType(@namespace, observer));

    /// <summary>
    /// Observes the observers consuming an event type across a set of namespaces, as one combined stream.
    /// </summary>
    /// <param name="eventStore">The event store the namespaces belong to.</param>
    /// <param name="eventTypeId">The identifier of the event type to filter by.</param>
    /// <param name="namespaces">The namespaces to observe.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read observers from.</param>
    /// <returns>An observable of the observers consuming the event type across every namespace.</returns>
    internal static IObservable<IEnumerable<ObserverInformationForEventType>> ObserveForAllNamespaces(
        string eventStore,
        string eventTypeId,
        IEnumerable<string> namespaces,
        IStorage storage)
    {
        var namespaceList = namespaces.ToArray();

        if (namespaceList.Length == 0)
        {
            return Observable.Return(Array.Empty<ObserverInformationForEventType>().AsEnumerable());
        }

        var perNamespace = namespaceList.Select(@namespace =>
            ObserverInformation
                .ObserveObservers(eventStore, @namespace, storage)
                .Select(observers => ObserverInformationForEventTypeFilter.FilterByEventType(@namespace, observers, eventTypeId)));

        return perNamespace.CombineLatest().Select(results => results.SelectMany(_ => _));
    }
}
