// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using User = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Marks user principals issued by Identity for cookies and Identity bearer tokens.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="options">The Identity options.</param>
public class ChronicleUserClaimsPrincipalFactory(UserManager<User> userManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<User>(userManager, options)
{
    /// <inheritdoc/>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User));
        identity.AddClaim(new Claim(ChronicleClaims.PrincipalKind, ChronicleClaims.User));
        return identity;
    }
}
