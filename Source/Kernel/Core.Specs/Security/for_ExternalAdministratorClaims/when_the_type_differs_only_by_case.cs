// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims;

public class when_the_type_differs_only_by_case : given.a_mapping
{
    void Establish() => _principal = External(new Claim("Roles", "chronicle-admin"));

    void Because() => _granted = ExternalAdministratorClaims.Grants(_principal, _mapping);

    [Fact] void should_deny() => _granted.ShouldBeFalse();
}
