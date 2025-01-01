// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactive;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
/// <param name="namespaces"><see cref="INamespaces"/> for working with namespaces.</param>
[Route("/api/event-store/{eventStore}/namespaces")]
public class NamespaceQueries(INamespaces namespaces) : ControllerBase
{
    /// <summary>
    /// Observes all namespaces registered.
    /// </summary>
    /// <param name="eventStore">The event store to observe namespaces for.</param>
    /// <returns>An observable for observing a collection of namespace names.</returns>
    [HttpGet]
    public ISubject<IEnumerable<string>> AllNamespaces([FromRoute] string eventStore)
    {
        var subject = new Subject<IEnumerable<string>>();
        namespaces.ObserveNamespaces(new() { EventStore = eventStore }).Subscribe(subject);
        return subject;
    }
}
