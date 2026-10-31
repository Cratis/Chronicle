// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_refreshing_a_password_token : given.a_token_request
{
    void Establish()
    {
        _request.GrantType = OpenIddictConstants.GrantTypes.RefreshToken;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(OpenIddictConstants.Claims.Subject, _user.Id.ToString()), new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Chronicle"));
        _authentication.AuthenticateAsync(Arg.Any<HttpContext>(), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            .Returns(AuthenticateResult.Success(new AuthenticationTicket(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)));
    }

    async Task Because() => _result = (SignInResult)await _controller.Exchange();
    [Fact] void should_keep_the_user_marker() => _result.Principal.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User).ShouldBeTrue();
    [Fact] void should_include_the_marker_in_the_new_access_token() => _result.Principal.FindFirst(ChronicleClaims.PrincipalKind).HasDestination(OpenIddictConstants.Destinations.AccessToken).ShouldBeTrue();
}
