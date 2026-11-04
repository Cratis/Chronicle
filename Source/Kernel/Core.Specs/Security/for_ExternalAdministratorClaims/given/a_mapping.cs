// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_ExternalAdministratorClaims.given;

public class a_mapping : Specification
{
    protected ExternalAdministratorClaim? _mapping;
    protected ClaimsPrincipal _principal;
    protected bool _granted;

    protected static Claim Mapped(string type, string value, string shortType)
    {
        var claim = new Claim(type, value);
        claim.Properties[ExternalAuthority.ShortClaimTypeProperty] = shortType;
        return claim;
    }

    protected static ClaimsPrincipal Principal(string? authenticationType, params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType));

    protected static ClaimsPrincipal External(params Claim[] claims) => Principal(ExternalAuthority.AuthenticationType, claims);

    void Establish() => _mapping = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" };
}
