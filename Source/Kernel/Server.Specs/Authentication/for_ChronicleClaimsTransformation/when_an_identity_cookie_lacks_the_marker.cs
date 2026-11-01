// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation;

public class when_an_identity_cookie_lacks_the_marker : given.a_claims_transformation
{
    ClaimsPrincipal _principal;

    void Establish() => _principal = new(IdentityOf(IdentityConstants.ApplicationScheme));

    async Task Because() => _result = await _transformation.TransformAsync(_principal);

    [Fact] void should_mark_it_as_a_user() => MarkerCount(_result).ShouldEqual(1);
}
