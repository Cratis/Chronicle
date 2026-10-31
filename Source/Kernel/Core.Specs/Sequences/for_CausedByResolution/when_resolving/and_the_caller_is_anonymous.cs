// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_the_caller_is_anonymous : given.a_caller
{
    void Establish() => _caller = new ClaimsPrincipal();
    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_use_the_supplied_identity() => _result.Subject.ShouldEqual("other");
}
