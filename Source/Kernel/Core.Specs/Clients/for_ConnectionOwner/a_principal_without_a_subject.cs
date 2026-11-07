// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class a_principal_without_a_subject : Specification
{
    ConnectionOwner _result;

    void Because() => _result = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "alice")], "test")));

    [Fact] void should_be_unidentified() => _result.IsUnidentified.ShouldBeTrue();
}
