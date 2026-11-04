// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AuthorizationDecisions;

public class when_the_administrator_lookup_throws : given.a_decision_point
{
    Exception? _thrown;

    void Establish() => _administrators.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns<bool>(_ => throw new InvalidOperationException("lookup failed"));

    async Task Because() => _thrown = await Catch.Exception(async () => _decision = await Decide(_principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel));

    [Fact] void should_not_throw() => _thrown.ShouldBeNull();
    [Fact] void should_deny_as_failed() => ShouldHaveDecided(AuthorizationOutcome.Denied, AuthorizationReason.Failed);
    [Fact] void should_log_the_capability() => _logger.Entries.Single().Message.ShouldContain(WellKnownCapabilities.AdministerSecurity.Value);
    [Fact] void should_not_log_secrets() => _logger.Entries.Single().Message.ShouldNotContain(Secret);
}
