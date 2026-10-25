// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Authorizes only authenticated internal-authority users with a current roster grant.
/// </summary>
/// <param name="options">The server configuration.</param>
/// <param name="privileges">The authoritative privilege lookup.</param>
/// <param name="logger">The logger.</param>
public class AdministratorAuthorization(IOptions<ChronicleOptions> options, IAdministratorPrivileges privileges, ILogger<AdministratorAuthorization> logger) : IAdministratorAuthorization
{
    /// <inheritdoc/>
    public async Task<bool> IsAuthorized(ClaimsPrincipal principal)
    {
        try
        {
            var authentication = options.Value.Authentication;
            if (!authentication.Enabled) return true;
            if (principal.Identity?.IsAuthenticated is not true) return false;
            if (!principal.HasClaim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)) return false;
            if (!authentication.UseInternalAuthority) return false;
            var subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(subject, out var userId)) return false;
            return await privileges.IsActiveAdministrator(userId);
        }
        catch (Exception exception)
        {
            logger.AdministratorCheckFailed(exception);
            return false;
        }
    }
}
