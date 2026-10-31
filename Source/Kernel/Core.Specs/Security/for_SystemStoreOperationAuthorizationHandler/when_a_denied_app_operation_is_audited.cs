// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_a_denied_app_operation_is_audited : given.an_operation_policy
{
    Task Because() => AuthorizeOperation();

    [Fact] void should_allow_the_existing_operation() => _context.HasSucceeded.ShouldBeTrue();
    [Fact] void should_warn_once() => _logger.Entries.Single().Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_describe_a_would_deny() => _logger.Entries.Single().Message.ShouldContain("would deny");
    [Fact] void should_name_the_caller_kind() => _logger.Entries.Single().Message.ShouldContain("app");
    [Fact] void should_name_the_command() => _logger.Entries.Single().Message.ShouldContain("ReplayObserver");
    [Fact] void should_not_log_tokens_or_secrets_from_the_principal_or_command() => _logger.Entries.Single().Message.ShouldNotContain(Secret);
}
