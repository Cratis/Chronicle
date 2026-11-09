// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates or changes a consumer binding of a logical source name to a physical peer.
/// </summary>
/// <param name="Id">The binding id.</param>
/// <param name="LogicalSource">The logical source name consumer code refers to.</param>
/// <param name="Endpoint">The federation endpoint of the peer, an absolute https URI.</param>
/// <param name="ServerPin">The pinned server certificate, <c language="text">sha256:&lt;base64&gt;</c>.</param>
/// <param name="ClientCertificate">The name of the configured client certificate to present. A name, never a secret value.</param>
/// <param name="RemoteEventStore">The event store on the peer.</param>
/// <param name="RemoteNamespace">The namespace on the peer.</param>
/// <param name="TargetEventStore">The local event store whose inbox receives the events.</param>
/// <param name="TargetNamespace">The local namespace.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record BindFederationPeer(string Id, string LogicalSource, string Endpoint, string ServerPin, string ClientCertificate, string RemoteEventStore, string RemoteNamespace, string TargetEventStore, string TargetNamespace)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationRegistry"/> to bind with.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IFederationRegistry registry) => registry.Bind(new(
        Id,
        LogicalSource,
        Endpoint,
        ServerPin,
        ClientCertificate,
        RemoteEventStore,
        RemoteNamespace,
        TargetEventStore,
        TargetNamespace,
        FederationOrigin.Runtime));
}
