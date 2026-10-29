// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_with_a_configured_setup_token : given.a_new_administrator
{
    void Establish() => _authentication = new() { InitialAdminSetupToken = "operator-token" };
    Task Because() => Bootstrap();
    [Fact] void should_use_the_configured_token() => _tokens.DidNotReceive().Issue(Arg.Any<UserId>());
    [Fact] void should_not_log_the_configured_token() => _logger.Messages.ShouldNotContain(entry => entry.Message.Contains(_authentication.InitialAdminSetupToken));
    [Fact] void should_explain_that_the_configured_token_is_required() => _logger.Messages.ShouldContain(entry => entry.Message.Contains("requires the configured Authentication:InitialAdminSetupToken"));
}
