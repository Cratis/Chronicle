// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_an_empty_sub_and_a_name_identifier : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "", ClaimValueTypes.String, "issuer"), new Claim(ClaimTypes.NameIdentifier, "u1", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_use_the_name_identifier() => _result.Subject.ShouldEqual("u1");
    [Fact] void should_not_be_a_client() => _result.IsClient.ShouldBeFalse();
}
