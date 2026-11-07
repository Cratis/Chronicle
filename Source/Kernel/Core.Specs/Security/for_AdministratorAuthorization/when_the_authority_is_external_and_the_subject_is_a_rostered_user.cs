// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_authority_is_external_and_the_subject_is_a_rostered_user : given.an_authorization_check
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", _userId.ToString(), ClaimValueTypes.String, "https://identity.example")], ExternalAuthority.AuthenticationType));
        _authorization = CreateAuthorization(new Authentication
        {
            Authority = "https://identity.example",
            ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" }
        });
    }

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_without_the_external_administrator_claim() => _authorized.ShouldBeFalse();
}
