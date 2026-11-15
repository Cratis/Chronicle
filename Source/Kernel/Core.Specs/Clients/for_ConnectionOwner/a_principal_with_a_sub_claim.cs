// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_a_sub_claim : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_use_the_subject() => _result.Subject.ShouldEqual("alice");
    [Fact] void should_use_the_issuer() => _result.Issuer.ShouldEqual("issuer");
}
