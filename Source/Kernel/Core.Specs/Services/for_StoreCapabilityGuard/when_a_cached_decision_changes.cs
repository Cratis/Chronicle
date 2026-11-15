// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.given;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_a_cached_decision_changes
{
    [Fact]
    public async Task should_recheck_after_a_new_snapshot()
    {
        using var caller = new an_entitled_caller();
        var route = new QueryRouting("store", "ns");
        (await StoreCapabilityGuard.Permits(caller.Context, route)).ShouldBeTrue();
        caller.Revoke();
        (await StoreCapabilityGuard.Permits(caller.Context, route)).ShouldBeFalse();
    }

    [Fact]
    public async Task should_recheck_after_lease_expiry()
    {
        using var caller = new an_entitled_caller();
        var route = new QueryRouting("store", "ns");
        (await StoreCapabilityGuard.Permits(caller.Context, route)).ShouldBeTrue();
        caller.Clock.Advance(TimeSpan.FromMinutes(6));
        (await StoreCapabilityGuard.Permits(caller.Context, route)).ShouldBeFalse();
    }
}
