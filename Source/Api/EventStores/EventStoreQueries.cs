// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;

namespace Cratis.Api.EventStores;

/// <summary>
/// Represents the API for working with event stores.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with event stores.</param>
[Route("/api/event-stores")]
public class EventStoreQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all event stores registered.
    /// </summary>
    /// <returns>A collection of <see cref="EventStoreName"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<EventStoreName>> GetEventStores() => storage.GetEventStores();

    /// <summary>
    /// Observes all event stores registered..
    /// </summary>
    /// <returns>An observable for observing a collection of <see cref="EventStoreName"/>.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<EventStore>> AllEventStores() =>
        new TransformingSubject<IEnumerable<EventStoreName>, IEnumerable<EventStore>>(
            storage.ObserveEventStores(),
            _ => _.Select(_ => new EventStore(_, string.Empty)));
}
