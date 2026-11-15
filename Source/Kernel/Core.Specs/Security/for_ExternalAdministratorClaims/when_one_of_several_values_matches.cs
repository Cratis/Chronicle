// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims;

public class when_one_of_several_values_matches : given.a_mapping
{
    void Establish() => _principal = External(new Claim("roles", "reader"), new Claim("roles", "chronicle-admin"));

    void Because() => _granted = ExternalAdministratorClaims.Grants(_principal, _mapping);

    [Fact] void should_grant() => _granted.ShouldBeTrue();
}
