// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Reactive;

namespace Cratis.Chronicle.Api.Identities;

/// <summary>
/// Represents the API for querying identities.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/identities")]
public class IdentitiesQueries : ControllerBase
{
    readonly IIdentities _identities;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentitiesQueries"/> class.
    /// </summary>
    /// <param name="identities"><see cref="IIdentities"/> for working with identities.</param>
    internal IdentitiesQueries(IIdentities identities)
    {
        _identities = identities;
    }

    /// <summary>
    /// Gets all identities.
    /// </summary>
    /// <param name="eventStore">The event store to get identities for.</param>
    /// <param name="namespace">The namespace to get identities for.</param>
    /// <returns>Collection of identities.</returns>
    [HttpGet]
    public Task<IEnumerable<Identity>> GetIdentities(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        _identities.GetIdentities(new() { EventStore = eventStore, Namespace = @namespace });

    /// <summary>
    /// Observes all identities.
    /// </summary>
    /// <param name="eventStore">The event store to get identities for.</param>
    /// <param name="namespace">The namespace to get identities for.</param>
    /// <returns>Collection of identities.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<Identity>> AllIdentities(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        _identities.InvokeAndWrapWithSubject(token => _identities.ObserveIdentities(new() { EventStore = eventStore, Namespace = @namespace }, token));
}
