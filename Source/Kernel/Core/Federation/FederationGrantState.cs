// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The effective producer grant after folding all events and configuration.
/// </summary>
/// <param name="Id">The link id.</param>
/// <param name="ClientPin">The pinned client certificate of the peer.</param>
/// <param name="EventStore">The local event store whose outbox the peer may read.</param>
/// <param name="Namespaces">The namespaces the peer may read.</param>
/// <param name="AllPublic">Whether the grant covers all public event types.</param>
/// <param name="EventTypes">The specific event types granted when <paramref name="AllPublic"/> is false.</param>
/// <param name="Revoked">Whether the link is revoked.</param>
/// <param name="Origin">Where this came from, see <see cref="FederationOrigin"/>.</param>
public record FederationGrantState(
    string Id,
    string ClientPin,
    string EventStore,
    IReadOnlyList<string> Namespaces,
    bool AllPublic,
    IReadOnlyList<string> EventTypes,
    bool Revoked,
    string Origin);
