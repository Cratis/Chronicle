// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Api.EventSequences.for_IdentityConverters;

public class when_converting_an_authenticated_principal_without_a_subject : Specification
{
    ClaimsPrincipal _principal;
    Exception _exception;

    void Establish() =>
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "operator")],
            authenticationType: "Test"));

    void Because() => _exception = Catch.Exception(() => _principal.ToContract());

    [Fact] void should_reject_the_principal() => _exception.ShouldBeOfExactType<AuthenticatedUserHasNoSubject>();
}
