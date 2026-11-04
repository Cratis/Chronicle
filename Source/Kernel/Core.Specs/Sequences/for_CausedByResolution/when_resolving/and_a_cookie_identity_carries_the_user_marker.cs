// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_a_cookie_identity_carries_the_user_marker : given.a_caller
{
    void Establish() => _caller = new(new ClaimsIdentity(
        [new Claim("sub", "caller"), new Claim("name", "Caller"), new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Identity.Application"));

    void Because() => _result = _caller.ResolveCausedBy(_supplied);

    [Fact] void should_record_the_caller() => _result.Subject.ShouldEqual("caller");
    [Fact] void should_not_record_the_supplied_identity() => _result.Subject.ShouldNotEqual("other");
}
