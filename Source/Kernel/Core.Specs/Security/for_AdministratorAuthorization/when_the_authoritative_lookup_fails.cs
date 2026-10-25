// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_authoritative_lookup_fails : given.an_authorization_check
{
    void Establish() => _privileges.IsActiveAdministrator(_userId).Returns(Task.FromException<bool>(new InvalidOperationException("unavailable")));

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_fail_closed_without_throwing_to_the_caller() => _authorized.ShouldBeFalse();
}
