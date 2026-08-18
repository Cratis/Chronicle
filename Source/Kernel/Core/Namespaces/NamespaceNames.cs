// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Provides query access to the available namespaces within an event store.
/// </summary>
[ReadModel]
[BelongsTo(WellKnownServices.Namespaces)]
public record NamespaceNames()
{
    /// <summary>
    /// Gets all namespaces for the given event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read namespaces from.</param>
    /// <returns>A collection of namespace names.</returns>
    internal static async Task<IEnumerable<string>> AllNamespaces(string eventStore, IStorage storage)
    {
        var namespaces = await storage.GetEventStore(new Concepts.EventStoreName(eventStore)).Namespaces.GetAll();
        return namespaces.Select(_ => _.Name.Value).ToArray();
    }

    /// <summary>
    /// Observes all namespaces for the given event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe namespaces from.</param>
    /// <returns>An observable subject emitting collections of namespace names.</returns>
    internal static ISubject<IEnumerable<string>> ObserveNamespaces(string eventStore, IStorage storage) =>
        storage.GetEventStore(new Concepts.EventStoreName(eventStore))
            .Namespaces
            .ObserveAll()
            .TransformSubject<IEnumerable<NamespaceState>, IEnumerable<string>>(namespaces => namespaces.Select(n => (string)n.Name).ToArray());
}
