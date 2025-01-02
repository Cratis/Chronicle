// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactive;

namespace Cratis.Chronicle.Api.EventStores;

/// <summary>
/// Represents the API for working with event stores.
/// </summary>
/// <param name="eventStores">The <see cref="IEventStores"/> contract.</param>
[Route("/api/event-stores")]
public class EventStoreQueries(IEventStores eventStores) : ControllerBase
{
    /// <summary>
    /// Get all event stores registered.
    /// </summary>
    /// <returns>A collection of event store names.</returns>
    [HttpGet]
    public async Task<IEnumerable<string>> GetEventStores() => await eventStores.GetEventStores();

    /// <summary>
    /// Observes all event stores registered..
    /// </summary>
    /// <returns>An observable for observing a collection of event store names.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<string>> AllEventStores()
    {
        var subject = new Subject<IEnumerable<string>>();
        eventStores.ObserveEventStores().Subscribe(subject);
        return subject;
    }
}
