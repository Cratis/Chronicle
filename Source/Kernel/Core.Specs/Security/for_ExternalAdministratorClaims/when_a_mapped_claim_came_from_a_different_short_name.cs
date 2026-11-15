// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims;

public class when_a_mapped_claim_came_from_a_different_short_name : given.a_mapping
{
    void Establish() => _principal = External(Mapped(ClaimTypes.Role, "chronicle-admin", "role"));

    void Because() => _granted = ExternalAdministratorClaims.Grants(_principal, _mapping);

    [Fact] void should_deny() => _granted.ShouldBeFalse();
}
