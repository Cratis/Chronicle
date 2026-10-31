// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_a_marked_user_has_no_subject : given.a_caller
{
    Exception? _error;
    void Establish() => _caller = new(new ClaimsIdentity([new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Chronicle"));
    void Because() => _error = Catch.Exception(() => _caller.ResolveCausedBy(_supplied));
    [Fact] void should_reject_instead_of_using_the_supplied_identity() => _error.ShouldBeOfExactType<AuthenticatedUserHasNoSubject>();
}
