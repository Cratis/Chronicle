// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_anonymous_caller_operates_while_enforced : given.an_operation_policy
{
    void Establish()
    {
        _enforce = true;
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("access_token", Secret)]));
    }

    Task Because() => AuthorizeOperation();

    [Fact] void should_refuse_the_anonymous_caller() => _context.HasSucceeded.ShouldBeFalse();
    [Fact] void should_not_log_an_audit_entry() => _logger.Entries.ShouldBeEmpty();
}
