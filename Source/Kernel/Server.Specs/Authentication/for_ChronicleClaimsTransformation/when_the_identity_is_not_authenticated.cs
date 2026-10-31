// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation;

public class when_the_identity_is_not_authenticated : given.a_claims_transformation
{
    ClaimsPrincipal _principal;

    void Establish() => _principal = new(IdentityOf(null));

    async Task Because() => _result = await _transformation.TransformAsync(_principal);

    [Fact] void should_not_mark_it() => MarkerCount(_result).ShouldEqual(0);
}
