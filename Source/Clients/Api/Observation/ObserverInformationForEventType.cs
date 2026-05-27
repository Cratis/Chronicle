// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents an observer that consumes a specific event type, including the namespace it belongs to.
/// </summary>
/// <param name="Namespace">The namespace the observer is registered in.</param>
/// <param name="Observer">The <see cref="ObserverInformation"/> describing the observer.</param>
[ReadModel]
public record ObserverInformationForEventType(
    string Namespace,
    ObserverInformation Observer)
{
    /// <summary>
    /// Get all observers that consume a specific event type across all namespaces in an event store.
    /// </summary>
    /// <param name="eventStore">The event store to search.</param>
    /// <param name="eventTypeId">The identifier of the event type to find consuming observers for.</param>
    /// <param name="observers">The <see cref="IObservers"/> service used to query observers.</param>
    /// <param name="namespaces">The <see cref="INamespaces"/> service used to enumerate namespaces.</param>
    /// <returns>A collection of <see cref="ObserverInformationForEventType"/> describing observers consuming the event type.</returns>
    public static async Task<IEnumerable<ObserverInformationForEventType>> GetObserversForEventType(
        string eventStore,
        string eventTypeId,
        IObservers observers,
        INamespaces namespaces)
    {
        var namespaceNames = await namespaces.GetNamespaces(new() { EventStore = eventStore });
        var results = new List<ObserverInformationForEventType>();

        foreach (var @namespace in namespaceNames)
        {
            var observersInNamespace = await observers.GetObservers(new()
            {
                EventStore = eventStore,
                Namespace = @namespace
            });

            results.AddRange(observersInNamespace
                .Where(observer => observer.EventTypes.Any(eventType => string.Equals(eventType.Id, eventTypeId, StringComparison.Ordinal)))
                .Select(observer => new ObserverInformationForEventType(@namespace, observer.ToApi())));
        }

        return results;
    }
}
