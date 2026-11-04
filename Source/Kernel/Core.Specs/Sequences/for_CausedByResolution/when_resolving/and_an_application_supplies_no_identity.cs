// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.when_resolving;

public class and_an_application_supplies_no_identity : given.a_caller
{
    void Establish()
    {
        _caller = new(new ClaimsIdentity([new Claim("sub", "application")], "ClientCredentials"));
        _supplied = null;
    }

    void Because() => _result = _caller.ResolveCausedBy(_supplied);
    [Fact] void should_use_the_application() => _result.Subject.ShouldEqual("application");
}
