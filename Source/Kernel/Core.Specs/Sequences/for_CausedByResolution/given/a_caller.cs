// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Sequences.for_CausedByResolution.given;

public class a_caller : Specification
{
    protected ClaimsPrincipal? _caller;
    protected Identity? _supplied;
    protected Concepts.Identities.Identity _result;

    protected static ClaimsPrincipal ChronicleUser() => new(new ClaimsIdentity(
        [new Claim("sub", "caller"), new Claim("name", "Caller"), new Claim("preferred_username", "caller-name"), new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User)], "Chronicle"));

    void Establish() => _supplied = new("other", "Other", "other-name", null);
}
