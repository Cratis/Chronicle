// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementEnforcement;

public class when_applying_the_mode : Specification
{
    [Theory]
    [InlineData(AuthorizationReason.Granted, false, true)]
    [InlineData(AuthorizationReason.Granted, true, true)]
    [InlineData(AuthorizationReason.NotEnrolled, false, true)]
    [InlineData(AuthorizationReason.NotEnrolled, true, true)]
    [InlineData(AuthorizationReason.NotGranted, false, true)]
    [InlineData(AuthorizationReason.NotGranted, true, false)]
    [InlineData(AuthorizationReason.NoPrincipal, false, true)]
    [InlineData(AuthorizationReason.NoPrincipal, true, false)]
    [InlineData(AuthorizationReason.Failed, false, false)]
    [InlineData(AuthorizationReason.Failed, true, false)]
    [InlineData(AuthorizationReason.UnknownCapability, false, false)]
    [InlineData(AuthorizationReason.UnknownCapability, true, false)]
    public void should_apply_the_shared_rule(AuthorizationReason reason, bool enforce, bool expected)
    {
        var decision = reason is AuthorizationReason.Granted or AuthorizationReason.NotEnrolled
            ? AuthorizationDecision.Allow(reason, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"))
            : AuthorizationDecision.Deny(reason, WellKnownCapabilities.ReadEventStore, AuthorizationScope.ForEventStore("shop"));
        StoreEntitlementEnforcement.Permits(decision, enforce).ShouldEqual(expected);
    }
}
