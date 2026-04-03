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
    /// Observes all event store names.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe event stores from.</param>
    /// <returns>An observable subject emitting collections of event store names.</returns>
    internal static ISubject<IEnumerable<string>> AllEventStores(IStorage storage) =>
        storage.ObserveEventStores()
            .TransformSubject(stores => stores.Select(s => s.Value));
}
