// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation;

public class when_an_identity_cookie_already_has_the_marker : given.a_claims_transformation
{
    ClaimsPrincipal _principal;

    void Establish() => _principal = new(IdentityOf(IdentityConstants.ApplicationScheme, new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)));

    async Task Because() => _result = await _transformation.TransformAsync(_principal);

    [Fact] void should_not_duplicate_it() => MarkerCount(_result).ShouldEqual(1);
}
