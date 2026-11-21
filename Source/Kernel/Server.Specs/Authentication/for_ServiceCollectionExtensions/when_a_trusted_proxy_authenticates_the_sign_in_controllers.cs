// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Server.Authentication.Controllers;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_a_trusted_proxy_authenticates_the_sign_in_controllers : given.discovered_controllers
{
    IEnumerable<Type> _controllers;

    void Because() => _controllers = DiscoverControllers(authenticationEnabled: true, new HttpAuthentication { Mode = HttpAuthenticationMode.TrustedProxy });

    [Fact] void should_keep_the_token_endpoint_for_clients() => _controllers.ShouldContain(typeof(AuthorizationController));
    [Fact] void should_leave_out_the_password_sign_in() => _controllers.ShouldNotContain(typeof(LoginController));
}
