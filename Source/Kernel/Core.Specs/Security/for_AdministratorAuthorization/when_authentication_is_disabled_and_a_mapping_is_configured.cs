// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_authentication_is_disabled_and_a_mapping_is_configured : given.an_authorization_check
{
    static Authentication External(bool enabled = true) => new()
    {
        Enabled = enabled,
        Authority = "https://identity.example",
        ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" }
    };

    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _authorization = CreateAuthorization(External(enabled: false));
    }

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_allow() => _authorized.ShouldBeTrue();
}
