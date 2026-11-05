// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class the_same_client_from_different_issuers : Specification
{
    ConnectionOwner _first;
    ConnectionOwner _second;

    void Because()
    {
        _first = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("azp", "app", ClaimValueTypes.String, "one")], "test")));
        _second = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("azp", "app", ClaimValueTypes.String, "two")], "test")));
    }

    [Fact] void should_not_be_equal() => _first.Equals(_second).ShouldBeFalse();
}
