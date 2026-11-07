// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_with_only_a_client_id_claim : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("client_id", "app", ClaimValueTypes.String, "issuer")], "test")));

    [Fact] void should_use_the_client() => _result.Subject.ShouldEqual("app");
    [Fact] void should_be_a_client() => _result.IsClient.ShouldBeTrue();
}
