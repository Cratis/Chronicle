// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_bootstrapping_with_a_configured_password : given.a_new_administrator
{
    void Establish() => _authentication = new() { AdminUser = new() { Password = "configured-password" } };
    Task Because() => Bootstrap();
    [Fact] void should_not_issue_a_token() => _tokens.DidNotReceive().Issue(Arg.Any<UserId>());
    [Fact] void should_not_log_a_token() => _logger.Messages.ShouldNotContain(entry => entry.Message.Contains("setup token"));
}
