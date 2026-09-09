// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Sequences.for_IdentityConverters;

public class when_the_authenticated_actor_has_no_meaningful_subject : Specification
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    void should_reject_instead_of_inventing_an_identity(string? subject)
    {
        var identity = new ClaimsIdentity("Test");
        if (subject is not null) identity.AddClaim(new Claim("sub", subject));
        Catch.Exception(() => new ClaimsPrincipal(identity).ToIdentity()).ShouldBeOfExactType<AuthenticatedUserHasNoSubject>();
    }
}
