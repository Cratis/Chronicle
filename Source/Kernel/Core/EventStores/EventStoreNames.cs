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
[ReadModel]
[BelongsTo(WellKnownServices.EventStores)]
public record EventStoreNames()
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
    internal static async Task<IEnumerable<string>> AllEventStores(IStorage storage)
    {
        var eventStores = await storage.GetEventStores();
        return eventStores.Select(_ => _.Value).ToArray();
    }

    /// <summary>
    /// Observes all event store names.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe event stores from.</param>
    /// <returns>An observable subject emitting collections of event store names.</returns>
    internal static ISubject<IEnumerable<string>> ObserveEventStores(IStorage storage) =>
        storage.ObserveEventStores()
            .TransformSubject(stores => stores.Select(s => s.Value));
}
