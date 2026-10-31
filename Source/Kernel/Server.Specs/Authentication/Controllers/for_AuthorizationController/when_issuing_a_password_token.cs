// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.Controllers.for_AuthorizationController;

public class when_issuing_a_password_token : given.a_token_request
{
    async Task Because() => _result = (SignInResult)await _controller.Exchange();
    [Fact] void should_mark_the_user_principal() => _result.Principal.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User).ShouldBeTrue();
    [Fact] void should_include_the_marker_in_the_access_token() => _result.Principal.FindFirst(ChronicleClaims.PrincipalKind).HasDestination(OpenIddictConstants.Destinations.AccessToken).ShouldBeTrue();
}
