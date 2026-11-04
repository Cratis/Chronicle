// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims;

public class when_the_mapping_is_blank : given.a_mapping
{
    void Establish()
    {
        _mapping = new ExternalAdministratorClaim { Type = " ", Value = " " };
        _principal = External(new Claim("", ""), new Claim("roles", " "));
    }

    void Because() => _granted = ExternalAdministratorClaims.Grants(_principal, _mapping);

    [Fact] void should_deny() => _granted.ShouldBeFalse();
}
