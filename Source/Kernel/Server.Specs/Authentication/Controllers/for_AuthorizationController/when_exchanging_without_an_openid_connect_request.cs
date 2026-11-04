// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController.given;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_exchanging_without_an_openid_connect_request : a_client_credentials_token_endpoint
{
    Exception _result;

    void Establish() => _controller.ControllerContext.HttpContext.Features.Set<global::OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreFeature>(null);

    async Task Because() => _result = await Catch.Exception(_controller.Exchange);

    [Fact] void should_throw_invalid_operation() => _result.ShouldBeOfExactType<InvalidOperationException>();
}
