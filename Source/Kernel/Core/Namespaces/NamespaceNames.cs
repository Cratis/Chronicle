// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Provides query access to the available namespaces within an event store.
/// </summary>
[ReadModel]
[BelongsTo(WellKnownServices.Namespaces)]
public record NamespaceNames()
{
    /// <summary>
    /// Observes all namespaces for the given event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe namespaces from.</param>
    /// <returns>An observable subject emitting collections of namespace names.</returns>
    internal static ISubject<IEnumerable<string>> AllNamespaces(string eventStore, IStorage storage) =>
        storage.GetEventStore(new Concepts.EventStoreName(eventStore))
            .Namespaces
            .ObserveAll()
            .TransformSubject(namespaces => namespaces.Select(n => (string)n.Name));
}
