// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims;

public class when_the_role_claim_type_was_mapped_inbound : given.a_mapping
{
    void Establish() => _principal = External(Mapped(ClaimTypes.Role, "chronicle-admin", "roles"));

    void Because() => _granted = ExternalAdministratorClaims.Grants(_principal, _mapping);

    [Fact] void should_grant() => _granted.ShouldBeTrue();
}
