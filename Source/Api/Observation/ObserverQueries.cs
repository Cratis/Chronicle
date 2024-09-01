// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Api.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
[Route("/api/event-store/{eventStore}/{namespace}/observers")]
public class ObserverQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("all-observers")]
    public Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>An observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("all-observers/observe")]
    public ISubject<IEnumerable<ObserverInformation>> AllObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        return namespaceStorage.Observers.ObserveAll();
    }
}
