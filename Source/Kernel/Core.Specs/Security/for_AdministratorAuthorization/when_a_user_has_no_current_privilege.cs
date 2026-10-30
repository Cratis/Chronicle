// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_a_user_has_no_current_privilege : given.an_authorization_check
{
    void Establish() => _privileges.IsActiveAdministrator(_userId).Returns(false);

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_the_user() => _authorized.ShouldBeFalse();
}
