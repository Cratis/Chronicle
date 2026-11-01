// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreAccess;

public class when_checking_whether_a_caller_is_allowed : Specification
{
    IAdministratorAuthorization _authorization;
    SystemStoreAccessVerdict _verdict;
    ClaimsPrincipal? _received;
    bool _allowed;

    void Establish()
    {
        _authorization = Substitute.For<IAdministratorAuthorization>();
        _authorization.IsAuthorized(Arg.Do<ClaimsPrincipal>(principal => _received = principal)).Returns(false);
        _verdict = new SystemStoreAccessVerdict(_authorization);
    }

    async Task Because() => _allowed = await _verdict.IsAllowed(null);

    [Fact] void should_treat_a_missing_caller_as_anonymous() => _received!.Identity!.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_refuse() => _allowed.ShouldBeFalse();
}
