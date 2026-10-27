// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_an_app_subject_matches_an_active_administrator : given.an_authorization_check
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", _userId.ToString()), new Claim("client_id", "app-client"), new Claim("grant_type", "client_credentials")], "Bearer"));

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_the_app_even_though_its_subject_is_an_administrator() => _authorized.ShouldBeFalse();
    [Fact] void should_not_consult_the_roster_for_an_app() => _privileges.DidNotReceive().IsActiveAdministrator(_userId);
}
