// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_the_query_declares_no_capability : given.a_store_capability_caller
{
    bool _permitted;

    void Establish() => Decide(AuthorizationReason.Granted, true);

    async Task Because() => _permitted = await StoreCapabilityGuard.Permits(_context, new("shop"));

    [Fact] void should_permit_the_query() => _permitted.ShouldBeTrue();
    [Fact] void should_decide_read_access_at_the_store() => _asked.Single().ShouldEqual((WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop")));
}
