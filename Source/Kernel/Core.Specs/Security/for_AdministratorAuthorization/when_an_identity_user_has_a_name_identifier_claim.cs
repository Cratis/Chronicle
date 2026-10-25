// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_an_identity_user_has_a_name_identifier_claim : given.an_authorization_check
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, _userId.ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "Identity.Application"));

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_accept_the_identity_user_identifier() => _authorized.ShouldBeTrue();
}
