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
public class ChronicleClaimsTransformation(UserManager<User> userManager) : IClaimsTransformation
{
    /// <inheritdoc/>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated ?? false)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;

            var subjectClaim = claimsIdentity.FindFirst("sub");
            if (subjectClaim is null || string.IsNullOrWhiteSpace(subjectClaim.Value))
            {
                if (subjectClaim is not null)
                {
                    claimsIdentity.RemoveClaim(subjectClaim);
                }

                var userId = userManager.GetUserId(principal);
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    claimsIdentity.AddClaim(new Claim("sub", userId));
                }
            }

            var nameClaim = claimsIdentity.FindFirst("name");
            if (nameClaim is null || string.IsNullOrWhiteSpace(nameClaim.Value))
            {
                if (nameClaim is not null)
                {
                    claimsIdentity.RemoveClaim(nameClaim);
                }

                var userName = userManager.GetUserName(principal);
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    claimsIdentity.AddClaim(new Claim("name", userName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
