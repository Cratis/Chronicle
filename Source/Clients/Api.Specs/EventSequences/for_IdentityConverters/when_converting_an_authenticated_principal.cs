// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Api.EventSequences.for_IdentityConverters;

public class when_converting_an_authenticated_principal : Specification
{
    ClaimsPrincipal _principal;
    Contracts.Identities.Identity _result;

    void Establish() =>
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "operator-id"),
                new Claim(ClaimTypes.Name, "Operator"),
                new Claim("preferred_username", "operator")
            ],
            authenticationType: "Test"));

    void Because() => _result = _principal.ToContract();

    [Fact] void should_use_the_stable_subject() => _result.Subject.ShouldEqual("operator-id");
    [Fact] void should_use_the_name() => _result.Name.ShouldEqual("Operator");
    [Fact] void should_use_the_preferred_username() => _result.UserName.ShouldEqual("operator");
}
