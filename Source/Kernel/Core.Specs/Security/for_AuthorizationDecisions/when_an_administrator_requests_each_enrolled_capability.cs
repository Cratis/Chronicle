// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_an_administrator_requests_each_enrolled_capability : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(_principal).Returns(true);

    [Theory]
    [MemberData(nameof(EnrolledCapabilities))]
    public async Task should_allow_at_kernel_scope(string capability)
    {
        _decision = await Decide(_principal, capability, AuthorizationScope.Kernel);
        ShouldHaveDecided(AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }

    [Theory]
    [MemberData(nameof(EnrolledCapabilities))]
    public async Task should_allow_at_system_scope(string capability)
    {
        _decision = await Decide(_principal, capability, AuthorizationScope.ForEventStore(EventStoreName.System));
        ShouldHaveDecided(AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }
}
