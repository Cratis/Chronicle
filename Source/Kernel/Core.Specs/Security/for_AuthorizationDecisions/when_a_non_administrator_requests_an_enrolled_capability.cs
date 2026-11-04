// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_a_non_administrator_requests_an_enrolled_capability : given.a_decision_point
{
    void Establish() => _administrators.IsAuthorized(_principal).Returns(false);

    [Theory]
    [MemberData(nameof(EnrolledCapabilities))]
    public async Task should_deny_as_not_granted(string capability)
    {
        _decision = await Decide(_principal, capability, AuthorizationScope.Kernel);
        ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }
}
