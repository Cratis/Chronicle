// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
[Route("/api/event-store/{eventStore}/namespaces")]
public class NamespaceQueries : ControllerBase
{
    readonly INamespaces _namespaces;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceQueries"/> class.
    /// </summary>
    /// <param name="namespaces"><see cref="INamespaces"/> for working with namespaces.</param>
    internal NamespaceQueries(INamespaces namespaces)
    {
        _namespaces = namespaces;
    }

    /// <summary>
    /// Observes all namespaces registered.
    /// </summary>
    /// <param name="eventStore">The event store to observe namespaces for.</param>
    /// <returns>An observable for observing a collection of namespace names.</returns>
    [HttpGet]
    public ISubject<IEnumerable<string>> AllNamespaces([FromRoute] string eventStore) =>
        _namespaces.InvokeAndWrapWithSubject(token => _namespaces.ObserveNamespaces(new() { EventStore = eventStore }, token));
}
