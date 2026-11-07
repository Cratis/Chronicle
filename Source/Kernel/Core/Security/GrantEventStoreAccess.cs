// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Security;

/// <summary>Explicitly grants ordinary event store access.</summary>
/// <param name="Holder">The exact issuer-qualified principal.</param>
/// <param name="EventStore">The event store, or null when AllEventStores is set.</param>
/// <param name="AllEventStores">Whether all ordinary stores are explicitly selected.</param>
/// <param name="Namespace">The namespace, or null for all namespaces.</param>
/// <param name="Access">The access flags.</param>
[Command]
[BelongsTo(WellKnownServices.Administrators)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record GrantEventStoreAccess(EventStoreAccessHolder Holder, EventStoreName? EventStore, bool AllEventStores, EventStoreNamespaceName? Namespace, StoreAccess Access)
{
    /// <summary>Persists an explicit command-sourced grant.</summary>
    /// <param name="grains">The grain factory.</param>
    /// <returns>The completion.</returns>
    public Task Handle(IGrainFactory grains) => grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Grant(new(new(Holder.Issuer, Holder.Subject, Holder.IsClient), EventStore, Namespace, Access, EventStoreAccessGrantSource.Command));
}
