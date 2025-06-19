// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/observers")]
public class ObserverQueries : ControllerBase
{
    readonly IObservers _observers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverQueries"/> class.
    /// </summary>
    /// <param name="observers"><see cref="IObservers"/> for working with observers.</param>
    internal ObserverQueries(IObservers observers)
    {
        _observers = observers;
    }

    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("all-observers")]
    public async Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        (await _observers.GetObservers(new() { EventStore = eventStore, Namespace = @namespace })).ToApi();

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>An observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("all-observers/observe")]
    public ISubject<IEnumerable<ObserverInformation>> AllObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        _observers.InvokeAndWrapWithTransformSubject(
            token => _observers.ObserveObservers(new() { EventStore = eventStore, Namespace = @namespace }, token),
            observers => observers.ToApi());
}
