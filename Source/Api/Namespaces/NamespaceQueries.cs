// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with namespaces.</param>
[Route("/api/event-store/{eventStore}/namespaces")]
public class NamespaceQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Observes all namespaces registered.
    /// </summary>
    /// <param name="eventStore">The event store to observe namespaces for.</param>
    /// <returns>An observable for observing a collection of <see cref="Namespace"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<Namespace>> AllNamespaces([FromRoute] EventStoreName eventStore)
    {
        var store = storage.GetEventStore(eventStore);
        return new TransformingSubject<IEnumerable<NamespaceState>, IEnumerable<Namespace>>(
            store.Namespaces.ObserveAll(),
            _ => _.Select(_ => new Namespace(_.Id, _.Name, string.Empty)));
    }
}
