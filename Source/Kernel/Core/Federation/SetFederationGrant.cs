// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates or changes a producer grant, or reinstates a revoked link.
/// </summary>
/// <param name="Id">The link id.</param>
/// <param name="ClientPin">The pinned client certificate of the peer, <c language="text">sha256:&lt;base64&gt;</c>.</param>
/// <param name="EventStore">The local event store whose outbox the peer may read.</param>
/// <param name="Namespaces">The namespaces the peer may read.</param>
/// <param name="AllPublic">Whether the grant covers all public event types.</param>
/// <param name="EventTypes">The specific event types granted when <paramref name="AllPublic"/> is false.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record SetFederationGrant(string Id, string ClientPin, string EventStore, IEnumerable<string> Namespaces, bool AllPublic, IEnumerable<string> EventTypes)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationRegistry"/> to set the grant with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationRegistry registry) => registry.SetGrant(new(
        Id,
        ClientPin,
        EventStore,
        [.. Namespaces],
        AllPublic,
        [.. EventTypes],
        false,
        FederationOrigin.Runtime));
}
