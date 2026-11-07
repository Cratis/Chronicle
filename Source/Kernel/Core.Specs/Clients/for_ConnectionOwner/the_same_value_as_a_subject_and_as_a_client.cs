// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients.for_ConnectionOwner;

public class the_same_value_as_a_subject_and_as_a_client : Specification
{
    ConnectionOwner _subject;
    ConnectionOwner _client;

    void Because()
    {
        _subject = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "x", ClaimValueTypes.String, "issuer")], "test")));
        _client = ConnectionOwner.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("azp", "x", ClaimValueTypes.String, "issuer")], "test")));
    }

    [Fact] void should_not_be_equal() => _subject.Equals(_client).ShouldBeFalse();
}
