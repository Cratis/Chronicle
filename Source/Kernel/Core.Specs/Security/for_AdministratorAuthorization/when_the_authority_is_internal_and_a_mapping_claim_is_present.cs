// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_authority_is_internal_and_a_mapping_claim_is_present : given.an_authorization_check
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("roles", "chronicle-admin"), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], ExternalAuthority.AuthenticationType));
        _authorization = CreateAuthorization(new Authentication { ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" } });
    }

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny() => _authorized.ShouldBeFalse();
}
