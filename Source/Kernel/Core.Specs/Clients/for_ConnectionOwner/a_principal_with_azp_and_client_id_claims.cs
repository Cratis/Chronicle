// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_azp_and_client_id_claims : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("azp", "from-azp", ClaimValueTypes.String, "issuer"), new Claim("client_id", "from-client-id", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_prefer_azp() => _result.Subject.ShouldEqual("from-azp");
}
