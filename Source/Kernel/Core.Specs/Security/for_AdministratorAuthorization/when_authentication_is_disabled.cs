// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_authentication_is_disabled : given.an_authorization_check
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _authorization = CreateAuthorization(new Authentication { Enabled = false });
    }

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_allow_an_anonymous_caller() => _authorized.ShouldBeTrue();
}
