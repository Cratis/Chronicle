// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security.for_AdministratorRoster.given;

namespace Cratis.Chronicle.Security.for_EventStoreEntitlements.given;

public class an_entitlements_grain : an_administrator_roster
{
    protected StoreEntitlements _entitlements;
    protected IStoreEntitlementsCacheClient _broadcast;
    protected EventStoreAccessGrant _grant;

    void Establish()
    {
        _broadcast = Substitute.For<IStoreEntitlementsCacheClient>();
        _grant = new(new(PrincipalKey.LocalIssuer, "app"), "shop", null, StoreAccess.Read, EventStoreAccessGrantSource.Command);
        _entitlements = CreateEntitlements();
    }

    protected StoreEntitlements CreateEntitlements() => new(_storage, _grainFactory, _serializer, _proof, _broadcast);
}
