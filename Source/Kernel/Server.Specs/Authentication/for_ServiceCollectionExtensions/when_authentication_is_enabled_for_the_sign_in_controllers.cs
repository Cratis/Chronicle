// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.Controllers;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_authentication_is_enabled_for_the_sign_in_controllers : given.discovered_controllers
{
    IEnumerable<Type> _controllers;

    void Because() => _controllers = DiscoverControllers(authenticationEnabled: true);

    [Fact] void should_map_the_token_endpoint_controller() => _controllers.ShouldContain(typeof(AuthorizationController));
    [Fact] void should_map_the_login_controller() => _controllers.ShouldContain(typeof(LoginController));
}
