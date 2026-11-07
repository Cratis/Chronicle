// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_PrincipalKey;

public class when_a_client_has_no_subject : given.principals
{
    PrincipalKey _key;

    void Because() => _key = PrincipalKey.From(new ClaimsPrincipal(new ClaimsIdentity([new Claim("azp", "app", ClaimValueTypes.String, ExternalIssuer)], "spec")), _external);

    [Fact] void should_identify_the_client() => _key.IsClient.ShouldBeTrue();
    [Fact] void should_mark_the_client_in_the_text() => _key.ToString().ShouldEqual($"{ExternalIssuer}|app|client");
}
