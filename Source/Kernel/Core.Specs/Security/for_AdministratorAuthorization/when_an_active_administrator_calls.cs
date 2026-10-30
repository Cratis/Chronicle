// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_an_active_administrator_calls : given.an_authorization_check
{
    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_allow_the_user() => _authorized.ShouldBeTrue();
}
