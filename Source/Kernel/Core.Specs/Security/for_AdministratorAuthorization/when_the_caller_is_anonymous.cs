// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_caller_is_anonymous : given.an_authorization_check
{
    void Establish() => _principal = new ClaimsPrincipal(new ClaimsIdentity());

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_the_caller() => _authorized.ShouldBeFalse();
}
