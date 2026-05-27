// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Helpers for filtering observers by event type across namespaces.
/// </summary>
internal static class ObserverInformationForEventTypeFilter
{
    /// <summary>
    /// Filter the observers in a namespace by event type identifier and project them to the API representation.
    /// </summary>
    /// <param name="namespace">The namespace the observers belong to.</param>
    /// <param name="observersInNamespace">The observers in the namespace.</param>
    /// <param name="eventTypeId">The event type identifier to filter by.</param>
    /// <returns>A collection of <see cref="ObserverInformationForEventType"/> for observers consuming the event type.</returns>
    public static IEnumerable<ObserverInformationForEventType> FilterByEventType(
        string @namespace,
        IEnumerable<Contracts.Observation.ObserverInformation> observersInNamespace,
        string eventTypeId) =>
        observersInNamespace
            .Where(observer => observer.EventTypes.Any(eventType => string.Equals(eventType.Id, eventTypeId, StringComparison.Ordinal)))
            .Select(observer => new ObserverInformationForEventType(@namespace, observer.ToApi()));

    /// <summary>
    /// Build an observable that combines per-namespace observer streams and filters them by event type.
    /// </summary>
    /// <param name="eventStore">The event store the observers belong to.</param>
    /// <param name="eventTypeId">The event type identifier to filter by.</param>
    /// <param name="observers">The <see cref="IObservers"/> service used to observe observers.</param>
    /// <param name="namespaceNames">The namespaces to observe.</param>
    /// <returns>An observable that emits the combined filtered collection whenever any namespace's observers change.</returns>
    public static IObservable<IEnumerable<ObserverInformationForEventType>> ObserveForAllNamespaces(
        string eventStore,
        string eventTypeId,
        IObservers observers,
        IEnumerable<string> namespaceNames)
    {
        var namespaceList = namespaceNames.ToArray();
        if (namespaceList.Length == 0)
        {
            return Observable.Return(Array.Empty<ObserverInformationForEventType>().AsEnumerable());
        }

        var perNamespaceStreams = namespaceList.Select(@namespace =>
            observers
                .ObserveObservers(new() { EventStore = eventStore, Namespace = @namespace })
                .Select(observersInNamespace => FilterByEventType(@namespace, observersInNamespace, eventTypeId)));

        return Observable.CombineLatest(perNamespaceStreams).Select(perNamespace => perNamespace.SelectMany(_ => _));
    }
}
