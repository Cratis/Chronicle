// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization;

public class when_the_authority_is_external : given.an_authorization_check
{
    void Establish() => _authorization = CreateAuthorization(new Authentication { Authority = "https://identity.example" });

    async Task Because() => _authorized = await _authorization.IsAuthorized(_principal);

    [Fact] void should_deny_the_user_even_with_a_marker_and_matching_subject() => _authorized.ShouldBeFalse();
}
