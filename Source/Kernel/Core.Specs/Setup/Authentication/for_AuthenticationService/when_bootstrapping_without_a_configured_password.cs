// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_without_a_configured_password : given.a_new_administrator
{
    Task Because() => Bootstrap();
    [Fact] void should_issue_a_token_for_the_administrator() => _tokens.Received(1).Issue(_administrator.Id);
    [Fact] void should_log_the_token() => _logger.Messages.ShouldContain(entry => entry.Message.Contains("issued-token"));
}
