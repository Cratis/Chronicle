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
/// <param name="Id">The identity of the namespace, which is its name.</param>
/// <param name="Name">The name of the namespace.</param>
/// <remarks>
/// The identity is carried as <c>Id</c> because that is the property name Arc's observable delta matching keys on -
/// a live view of a model without one falls back to whole-payload matching, which never sees a replacement and
/// leaks removed rows.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Namespaces)]
public record NamespaceNames(string Id, string Name)
{
    /// <summary>
    /// Gets all namespaces for the given event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read namespaces from.</param>
    /// <returns>A collection of namespace names.</returns>
    internal static async Task<IEnumerable<NamespaceNames>> AllNamespaces(string eventStore, IStorage storage)
    {
        var namespaces = await storage.GetEventStore(new Concepts.EventStoreName(eventStore)).Namespaces.GetAll();
        return [.. namespaces.Select(@namespace => new NamespaceNames(@namespace.Name.Value, @namespace.Name.Value))];
    }

    /// <summary>
    /// Observes all namespaces for the given event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe namespaces from.</param>
    /// <returns>An observable subject emitting collections of namespace names.</returns>
    internal static ISubject<IEnumerable<NamespaceNames>> ObserveNamespaces(string eventStore, IStorage storage) =>
        storage.GetEventStore(new Concepts.EventStoreName(eventStore))
            .Namespaces
            .ObserveAll()
            .TransformSubject<IEnumerable<NamespaceState>, IEnumerable<NamespaceNames>>(
                namespaces => [.. namespaces.Select(@namespace => new NamespaceNames(@namespace.Name.Value, @namespace.Name.Value))]);
}
