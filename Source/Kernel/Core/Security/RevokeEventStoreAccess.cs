// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>Revokes a command-managed ordinary event store grant.</summary>
/// <param name="Holder">The exact issuer-qualified principal.</param>
/// <param name="EventStore">The event store, or null when AllEventStores is set.</param>
/// <param name="AllEventStores">Whether all ordinary stores are explicitly selected.</param>
/// <param name="Namespace">The namespace, or null for all namespaces.</param>
[Command]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record RevokeEventStoreAccess(EventStoreAccessHolder Holder, EventStoreName? EventStore, bool AllEventStores, EventStoreNamespaceName? Namespace)
{
    /// <summary>Revokes the identified grant. Configuration reconciliation may restore configured grants.</summary>
    /// <param name="grains">The grain factory.</param>
    /// <returns>Whether a grant was revoked.</returns>
    public Task<bool> Handle(IGrainFactory grains) => grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Revoke(new(new(Holder.Issuer, Holder.Subject, Holder.IsClient), EventStore, Namespace, StoreAccess.None, EventStoreAccessGrantSource.Command));
}
