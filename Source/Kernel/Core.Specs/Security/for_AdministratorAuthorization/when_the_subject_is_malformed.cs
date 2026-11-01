// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_subject_is_malformed : given.an_authorization_check
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "not-a-user-id"), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "spec"));

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_the_user() => _authorized.ShouldBeFalse();
}
