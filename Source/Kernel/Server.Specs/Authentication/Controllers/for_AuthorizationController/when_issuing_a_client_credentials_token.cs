// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_issuing_a_client_credentials_token : given.a_token_request
{
    void Establish()
    {
        _request.GrantType = OpenIddictConstants.GrantTypes.ClientCredentials;
        _request.ClientId = "application";
        var application = new object();
        _applications.FindByClientIdAsync("application").Returns(application);
        _applications.GetIdAsync(application).Returns(Guid.NewGuid().ToString());
        _applications.GetClientIdAsync(application).Returns("application");
    }

    async Task Because() => _result = (SignInResult)await _controller.Exchange();
    [Fact] void should_leave_the_application_unmarked() => _result.Principal.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User).ShouldBeFalse();
}
