// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Sequences.for_IdentityConverters;

public class when_the_actor_has_only_a_subject : Specification
{
    Concepts.Identities.Identity _result;

    void Because() => _result = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "Test")).ToIdentity();

    [Fact] void should_preserve_the_subject() => _result.Subject.ShouldEqual("operator");
    [Fact] void should_fall_back_to_the_subject_for_name() => _result.Name.ShouldEqual("operator");
    [Fact] void should_fall_back_to_the_subject_for_username() => _result.UserName.ShouldEqual("operator");
}
