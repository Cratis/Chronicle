// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_sub_and_azp_claims : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice", ClaimValueTypes.String, "issuer"), new Claim("azp", "app", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_prefer_the_subject() => _result.Subject.ShouldEqual("alice");
    [Fact] void should_not_be_a_client() => _result.IsClient.ShouldBeFalse();
}
