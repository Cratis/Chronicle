// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_authority_is_external_and_the_mapped_claim_is_present : given.an_authorization_check
{
    static Authentication External(bool enabled = true) => new()
    {
        Enabled = enabled,
        Authority = "https://identity.example",
        ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" }
    };

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("roles", "chronicle-admin")], ExternalAuthority.AuthenticationType));
        _authorization = CreateAuthorization(External());
    }

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_authorize() => _authorized.ShouldBeTrue();
    [Fact] void should_not_consult_the_roster() => _privileges.DidNotReceive().IsActiveAdministrator(Arg.Any<UserId>());
}
