// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates or changes a consumer binding of a logical source name to a physical peer.
/// </summary>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = WellKnownAuthorizationPolicies.AdministratorOperations)]
[BelongsTo(WellKnownServices.Federation)]
public record BindFederationPeer(string Id, string LogicalSource, string Endpoint, string ServerPin, string ClientCertificate, string RemoteEventStore, string RemoteNamespace, string TargetEventStore, string TargetNamespace)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Handle() => throw new NotImplementedException();
}
