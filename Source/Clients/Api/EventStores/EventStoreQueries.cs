// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Security.Cryptography.Xml;
using Cratis.Chronicle.Contracts;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.EventStores;

/// <summary>
/// Represents the API for working with event stores.
/// </summary>
[Route("/api/event-stores")]
public class EventStoreQueries : ControllerBase
{
    readonly IEventStores _eventStores;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreQueries"/> class.
    /// </summary>
    /// <param name="eventStores">The <see cref="IEventStores"/> contract.</param>
    internal EventStoreQueries(IEventStores eventStores)
    {
        _eventStores = eventStores;
    }

    /// <summary>
    /// Get all event stores registered.
    /// </summary>
    /// <returns>A collection of event store names.</returns>
    [HttpGet]
    public async Task<IEnumerable<string>> GetEventStores() => await _eventStores.GetEventStores();

    /// <summary>
    /// Observes all event stores registered..
    /// </summary>
    /// <returns>An observable for observing a collection of event store names.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<string>> AllEventStores() =>
        _eventStores.InvokeAndWrapWithSubject(token => _eventStores.ObserveEventStores(token));
}
