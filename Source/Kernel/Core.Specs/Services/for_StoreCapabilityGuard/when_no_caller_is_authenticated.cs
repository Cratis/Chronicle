// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Services.given;

namespace Cratis.Chronicle.Services.for_StoreCapabilityGuard;

public class when_no_caller_is_authenticated
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task should_apply_the_configured_mode(bool enforce, bool allowed)
    {
        using var caller = new an_entitled_caller(enforce);
        caller.Http.User = new ClaimsPrincipal(new ClaimsIdentity());
        (await StoreCapabilityGuard.Permits(caller.Context, new QueryRouting("store", "ns"))).ShouldEqual(allowed);
    }
}
