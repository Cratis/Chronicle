// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_an_anonymous_operation_is_audited : given.an_operation_policy
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("access_token", Secret)]));

    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_existing_operation() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_name_the_anonymous_caller_and_operation() => _logger.Entries.Single().Message.ShouldContain("anonymous calling ReplayObserver");
    [Fact] void should_not_log_tokens_or_secrets() => _logger.Entries.Single().Message.ShouldNotContain(Secret);
}
