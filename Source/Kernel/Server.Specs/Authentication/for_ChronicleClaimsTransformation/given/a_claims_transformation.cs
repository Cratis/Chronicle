// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation.given;

public class a_claims_transformation : Specification
{
    protected ChronicleClaimsTransformation _transformation;
    protected ClaimsPrincipal _result;

    void Establish()
    {
        var userManager = new UserManager<Storage.Security.User>(
            Substitute.For<IUserStore<Storage.Security.User>>(),
            Options.Create(new IdentityOptions()),
            null!,
            [],
            [],
            null!,
            null!,
            null!,
            null!);
        _transformation = new(userManager);
    }

    protected static ClaimsIdentity IdentityOf(string? authenticationType, params Claim[] extra) =>
        new([new Claim("sub", "caller"), new Claim("name", "Caller"), .. extra], authenticationType);

    protected static int MarkerCount(ClaimsPrincipal principal) =>
        principal.Claims.Count(_ => _.Type == ChronicleClaims.PrincipalKind && _.Value == ChronicleClaims.User);
}
