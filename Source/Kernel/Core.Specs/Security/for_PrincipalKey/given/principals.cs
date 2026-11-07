// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_PrincipalKey.given;

public class principals : Specification
{
    protected const string ExternalIssuer = "https://identity.example";
    protected static readonly Authentication _internal = new();
    protected static readonly Authentication _external = new() { Authority = ExternalIssuer };

    protected static ClaimsPrincipal Principal(string issuer, string subject, params (string Type, string Value)[] extra) =>
        new(new ClaimsIdentity(
            [new Claim("sub", subject, ClaimValueTypes.String, issuer), .. extra.Select(_ => new Claim(_.Type, _.Value, ClaimValueTypes.String, issuer))],
            "spec"));
}
