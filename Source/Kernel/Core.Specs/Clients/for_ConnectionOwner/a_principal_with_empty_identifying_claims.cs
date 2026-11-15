// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_empty_identifying_claims : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "", ClaimValueTypes.String, "issuer"), new Claim("azp", "", ClaimValueTypes.String, "issuer"), new Claim("client_id", "", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_be_unidentified() => _result.IsUnidentified.ShouldBeTrue();
}
