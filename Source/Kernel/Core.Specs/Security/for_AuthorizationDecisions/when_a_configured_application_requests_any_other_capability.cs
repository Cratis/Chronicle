// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_a_configured_application_requests_any_other_capability : given.a_decision_point
{
    public static TheoryData<string> OtherCapabilities() => new()
    {
        WellKnownCapabilities.AdministerSecurity.Value,
        WellKnownCapabilities.OperateEventStore.Value,
        WellKnownCapabilities.AccessSystemStore.Value,
        WellKnownCapabilities.ReleaseCompliance.Value,
        WellKnownCapabilities.ManageComplianceKeys.Value,
        WellKnownCapabilities.ReadAlerts.Value
    };

    public static TheoryData<string> StoreCapabilities() => new()
    {
        WellKnownCapabilities.ReleaseCompliance.Value,
        WellKnownCapabilities.ManageComplianceKeys.Value,
        WellKnownCapabilities.ReadAlerts.Value,
        WellKnownCapabilities.ReadEventStore.Value,
        WellKnownCapabilities.AppendEventStore.Value,
        WellKnownCapabilities.ManageEventStore.Value
    };

    void Establish()
    {
        _enrollingApplications = ["billing-app"];
        _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);
    }

    [Theory]
    [MemberData(nameof(OtherCapabilities))]
    public async Task should_deny_it_at_kernel_scope(string capability)
    {
        var decision = await Decide(ApplicationToken("billing-app"), capability, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Theory]
    [MemberData(nameof(StoreCapabilities))]
    public async Task should_deny_it_in_a_store(string capability)
    {
        var decision = await Decide(ApplicationToken("billing-app"), capability, AuthorizationScope.ForNamespace("store", "namespace"));
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
