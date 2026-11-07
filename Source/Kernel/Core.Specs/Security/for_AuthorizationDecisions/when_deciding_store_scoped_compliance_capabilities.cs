// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_deciding_store_scoped_compliance_capabilities : given.a_decision_point
{
    public static TheoryData<string> Capabilities() => new()
    {
        "chronicle.compliance.release",
        "chronicle.compliance.key-lifecycle"
    };

    [Theory]
    [MemberData(nameof(Capabilities))]
    public async Task should_allow_an_administrator(string capability)
    {
        _administrators.IsAuthorized(_principal).Returns(true);
        _decision = await Decide(_principal, capability, AuthorizationScope.ForNamespace("store", "namespace"));
        ShouldHaveDecided(AuthorizationOutcome.Allowed, AuthorizationReason.Granted);
    }

    [Theory]
    [MemberData(nameof(Capabilities))]
    public async Task should_deny_a_non_administrator(string capability)
    {
        _decision = await Decide(_principal, capability, AuthorizationScope.ForNamespace("store", "namespace"));
        ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.NotGranted);
    }

    [Theory]
    [MemberData(nameof(Capabilities))]
    public async Task should_allow_when_authentication_is_disabled(string capability)
    {
        _authenticationEnabled = false;
        _decision = await Decide(null, capability, AuthorizationScope.ForNamespace("store", "namespace"));
        ShouldHaveDecided(AuthorizationOutcome.Allowed, AuthorizationReason.AuthenticationDisabled);
    }

    [Theory]
    [MemberData(nameof(Capabilities))]
    public async Task should_fail_closed_when_the_lookup_throws(string capability)
    {
        _administrators.IsAuthorized(_principal).Returns(Task.FromException<bool>(new InvalidOperationException("lookup unavailable")));
        _decision = await Decide(_principal, capability, AuthorizationScope.ForNamespace("store", "namespace"));
        ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.Failed);
    }
}
