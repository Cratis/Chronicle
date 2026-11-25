// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_a_proxy_identity_arrives_without_trusted_proxy_authentication : given.an_authorization_check
{
    void Establish() =>
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], TrustedProxyUsers.AuthenticationType));

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_not_authorize() => _authorized.ShouldBeFalse();
    [Fact] void should_not_consult_the_roster() => _privileges.DidNotReceive().IsActiveAdministrator(Arg.Any<UserId>());
}
