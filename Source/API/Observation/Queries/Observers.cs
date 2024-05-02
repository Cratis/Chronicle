// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Observation;
using Cratis.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Observation.Queries;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/{namespace}/observers")]
public class Observers() : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">The event store the observers are for.</param>
    /// <param name="namespace">The namespace within the event store the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<ObserverInformation>>> AllObservers(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace)
    {
        throw new NotImplementedException();
    }
}
