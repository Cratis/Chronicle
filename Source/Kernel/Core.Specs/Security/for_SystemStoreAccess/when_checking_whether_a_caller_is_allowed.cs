// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SystemStoreAccess;

public class when_checking_whether_a_caller_is_allowed : Specification
{
    IAdministratorAuthorization _authorization;
    IAuthorizationDecisions _decisions;
    SystemStoreAccessVerdict _verdict;
    bool _allowed;

    void Establish()
    {
        _authorization = Substitute.For<IAdministratorAuthorization>();
        _authorization.IsAuthorized(Arg.Any<ClaimsPrincipal>()).Returns(false);
        _decisions = new AuthorizationDecisions(_authorization, Options.Create(new ChronicleOptions()), NullLogger<AuthorizationDecisions>.Instance);
        _verdict = new SystemStoreAccessVerdict(_decisions);
    }

    async Task Because() => _allowed = await _verdict.IsAllowed(null);

    [Fact] void should_refuse_a_missing_caller() => _allowed.ShouldBeFalse();
    [Fact] void should_not_consult_the_administrator_lookup_for_a_missing_caller() => _authorization.DidNotReceiveWithAnyArgs().IsAuthorized(default!);
}
