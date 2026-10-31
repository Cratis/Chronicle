// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation;

public class when_a_cookie_identity_accompanies_an_external_identity : given.a_claims_transformation
{
    ClaimsPrincipal _principal;

    void Establish()
    {
        _principal = new(IdentityOf("Bearer"));
        _principal.AddIdentity(IdentityOf(IdentityConstants.ApplicationScheme));
    }

    async Task Because() => _result = await _transformation.TransformAsync(_principal);

    [Fact] void should_mark_only_the_cookie_identity() => _result.Identities.Where(_ => _.HasClaim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)).Select(_ => _.AuthenticationType).ShouldEqual([IdentityConstants.ApplicationScheme]);
}
