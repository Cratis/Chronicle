// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A producer-side grant was created or changed. The event source id is the link id.
/// </summary>
/// <param name="ClientPin">The pinned client certificate of the peer, <c language="text">sha256:&lt;base64&gt;</c>.</param>
/// <param name="EventStore">The local event store whose outbox the peer may read.</param>
/// <param name="Namespaces">The namespaces the peer may read.</param>
/// <param name="AllPublic">Whether the grant covers all public event types.</param>
/// <param name="EventTypes">The specific event types granted when <paramref name="AllPublic"/> is false.</param>
/// <param name="Origin">Where this came from, see <see cref="FederationOrigin"/>.</param>
[EventType]
public record FederationGrantSet(
    string ClientPin,
    string EventStore,
    IEnumerable<string> Namespaces,
    bool AllPublic,
    IEnumerable<string> EventTypes,
    string Origin);
