// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Applies per-node throttling to every Identity password sign-in path.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="contextAccessor">The request context accessor.</param>
/// <param name="claimsFactory">The claims factory.</param>
/// <param name="optionsAccessor">The Identity options.</param>
/// <param name="logger">The logger.</param>
/// <param name="schemes">The authentication schemes.</param>
/// <param name="confirmation">The user confirmation policy.</param>
/// <param name="throttle">The sign-in throttle.</param>
public class ChronicleSignInManager(
    UserManager<User> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<User>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation,
    ISignInThrottle throttle) : SignInManager<User>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    /// <inheritdoc/>
    public override async Task<SignInResult> CheckPasswordSignInAsync(User user, string password, bool lockoutOnFailure)
    {
        var username = user.Username.Value;
        var remote = Context.Connection.RemoteIpAddress;
        var address = remote?.MapToIPv6().ToString() ?? string.Empty;
        if (throttle.IsLockedOut(username, address))
        {
            Logger.SignInThrottled(username, address);
            return SignInResult.LockedOut;
        }

        var result = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        if (result.Succeeded) throttle.Succeeded(username, address);
        else throttle.Failed(username, address);

        return result;
    }
}
