// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_a_managing_application_requests_any_other_capability : given.a_decision_point
{
    public static TheoryData<string> OtherCapabilities() => new()
    {
        WellKnownCapabilities.AdministerSecurity.Value,
        WellKnownCapabilities.OperateEventStore.Value,
        WellKnownCapabilities.AccessSystemStore.Value,
        WellKnownCapabilities.EnrollFederation.Value,
        WellKnownCapabilities.ReleaseCompliance.Value,
        WellKnownCapabilities.ManageComplianceKeys.Value,
        WellKnownCapabilities.ReadAlerts.Value
    };

    void Establish()
    {
        _managingApplications = ["operator-app"];
        _administrators.IsAuthorized(default!).ReturnsForAnyArgs(false);
    }

    [Theory]
    [MemberData(nameof(OtherCapabilities))]
    public async Task should_deny_it_at_kernel_scope(string capability)
    {
        var decision = await Decide(ApplicationToken("operator-app"), capability, AuthorizationScope.Kernel);
        ShouldHaveDecided(decision, AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
