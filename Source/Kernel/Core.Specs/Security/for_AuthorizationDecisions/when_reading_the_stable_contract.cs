// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_reading_the_stable_contract : Specification
{
    [Fact] void should_pin_the_outcome_values() =>
        new[] { (int)AuthorizationOutcome.Denied, (int)AuthorizationOutcome.Allowed }.ShouldEqual([0, 1]);

    [Fact] void should_pin_the_reason_values() =>
        new[]
        {
            (int)AuthorizationReason.Failed,
            (int)AuthorizationReason.Granted,
            (int)AuthorizationReason.AuthenticationDisabled,
            (int)AuthorizationReason.NotEnrolled,
            (int)AuthorizationReason.NoPrincipal,
            (int)AuthorizationReason.NotGranted,
            (int)AuthorizationReason.UnknownCapability
        }.ShouldEqual([0, 1, 2, 3, 4, 5, 6]);

    [Fact] void should_default_to_denied_and_failed()
    {
        default(AuthorizationOutcome).ShouldEqual(AuthorizationOutcome.Denied);
        default(AuthorizationReason).ShouldEqual(AuthorizationReason.Failed);
    }

    [Fact] void should_pin_the_capability_names() =>
        new[]
        {
            WellKnownCapabilities.AdministerSecurity.Value,
            WellKnownCapabilities.OperateEventStore.Value,
            WellKnownCapabilities.AccessSystemStore.Value
        }.ShouldEqual(["chronicle.security.administer", "chronicle.event-store.operate", "chronicle.event-store.access"]);
}
