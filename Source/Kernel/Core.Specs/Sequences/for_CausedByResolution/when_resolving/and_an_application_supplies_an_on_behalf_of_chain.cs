// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_an_application_supplies_an_on_behalf_of_chain : given.a_caller
{
    void Establish()
    {
        _caller = new(new ClaimsIdentity([new Claim("sub", "application")], "ClientCredentials"));
        _supplied = new("agent", "Agent", "agent", new("middle", "Middle", "middle", _supplied));
    }

    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_keep_the_head() => _result.Subject.ShouldEqual("agent");
    [Fact] void should_keep_the_middle_link() => _result.OnBehalfOf.Subject.ShouldEqual("middle");
    [Fact] void should_keep_the_end_user() => _result.OnBehalfOf.OnBehalfOf.Subject.ShouldEqual("other");
}
