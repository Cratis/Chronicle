// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Transforms claims to include Chronicle-specific claims like the user ID as the "sub" claim.
/// </summary>
/// <param name="userManager">The user manager.</param>
public class ChronicleClaimsTransformation(UserManager<ChronicleUser> userManager) : IClaimsTransformation
{
    /// <inheritdoc/>
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated ?? false)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;

            // Only add sub claim if it doesn't already exist
            if (!claimsIdentity.HasClaim(c => c.Type == "sub"))
            {
                var userId = userManager.GetUserId(principal);
                if (!string.IsNullOrEmpty(userId))
                {
                    claimsIdentity.AddClaim(new Claim("sub", userId));
                }
            }

            // Add name claim if it doesn't exist
            if (!claimsIdentity.HasClaim(c => c.Type == "name"))
            {
                var userName = userManager.GetUserName(principal);
                if (!string.IsNullOrEmpty(userName))
                {
                    claimsIdentity.AddClaim(new Claim("name", userName));
                }
            }
        }

        return principal;
    }
}
