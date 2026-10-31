// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_restarting_after_the_administrator_has_logged_in : given.an_existing_administrator
{
    void Establish() => _administrator.HasLoggedIn = true;
    Task Because() => _service.EnsureDefaultAdminUser();
    [Fact] void should_not_issue_a_setup_token() => _tokens.DidNotReceive().Issue(Arg.Any<UserId>());
    [Fact] void should_not_log_a_setup_token() => _logger.Messages.ShouldNotContain(entry => entry.Message.Contains("setup token"));
}
