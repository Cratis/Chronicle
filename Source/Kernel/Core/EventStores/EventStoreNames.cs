// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Provides query access to the available event stores.
/// </summary>
/// <param name="Id">The identity of the event store, which is its name.</param>
/// <param name="Name">The name of the event store.</param>
/// <remarks>
/// The identity is carried as <c>Id</c> because that is the property name Arc's observable delta matching keys on -
/// a live view of a model without one falls back to whole-payload matching, which never sees a replacement and
/// leaks removed rows.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.EventStores)]
public record EventStoreNames(string Id, string Name)
{
    /// <summary>
    /// Gets all event store names.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read event stores from.</param>
    /// <returns>A collection of event store names.</returns>
    /// <remarks>
    /// A caller asking for the current list needs a snapshot, not the first value of a subscription.
    /// Observing implicitly registers the event store, so answering a snapshot through the observable
    /// changes what it is reporting on.
    /// </remarks>
    internal static async Task<IEnumerable<EventStoreNames>> AllEventStores(IStorage storage)
    {
        var eventStores = await storage.GetEventStores();
        return [.. eventStores.Select(eventStore => new EventStoreNames(eventStore.Value, eventStore.Value))];
    }

    /// <summary>
    /// Observes all event store names.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe event stores from.</param>
    /// <returns>An observable subject emitting collections of event store names.</returns>
    internal static ISubject<IEnumerable<EventStoreNames>> ObserveEventStores(IStorage storage) =>
        storage.ObserveEventStores()
            .TransformSubject<IEnumerable<Concepts.EventStoreName>, IEnumerable<EventStoreNames>>(
                stores => [.. stores.Select(eventStore => new EventStoreNames(eventStore.Value, eventStore.Value))]);
}
