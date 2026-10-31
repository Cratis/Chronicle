// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_restarting_with_an_unconfigured_administrator : given.an_existing_administrator
{
    Task Because() => _service.EnsureDefaultAdminUser();
    [Fact] void should_issue_a_setup_token() => _tokens.Received(1).Issue(_administrator.Id);
    [Fact] void should_log_the_token_for_the_operator() => _logger.Messages.ShouldContain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("issued-token"));
}
