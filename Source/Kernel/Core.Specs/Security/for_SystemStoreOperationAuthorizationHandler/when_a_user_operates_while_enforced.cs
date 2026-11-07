// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_user_operates_while_enforced : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        ((ClaimsIdentity)_principal.Identity!).AddClaim(new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User));
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_refuse_the_non_administrator_user() => _context.HasSucceeded.ShouldBeFalse();
    [Fact] void should_not_log_an_audit_entry() => _logger.Entries.ShouldBeEmpty();
}
