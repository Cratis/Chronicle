// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Cratis.Chronicle.Server.Authentication.Controllers;

/// <summary>
/// Controller for handling OAuth authorization requests.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="signInManager">The sign-in manager.</param>
/// <param name="applicationManager">The OpenIddict application manager.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("connect")]
[AllowAnonymous]
public class AuthorizationController(
    UserManager<ChronicleUser> userManager,
    SignInManager<ChronicleUser> signInManager,
    IOpenIddictApplicationManager applicationManager,
    ILogger<AuthorizationController> logger) : ControllerBase
{
    /// <summary>
    /// Handles token requests.
    /// </summary>
    /// <returns>The token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the OpenID Connect request cannot be retrieved or the grant type is not supported.</exception>
    [HttpPost("token")]
    [Produces("application/json")]
    [AspNetResult]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        logger.TokenEndpointCalled(request.GrantType);

        if (request.IsClientCredentialsGrantType())
        {
            logger.ProcessingClientCredentialsGrant(request.ClientId ?? string.Empty);

            // OpenIddict handles client validation automatically through ApplicationStore
            // We just need to create the claims identity
            var application = await applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty);
            if (application == null)
            {
                logger.ApplicationNotFound(request.ClientId ?? string.Empty);
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application was not found."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var applicationId = await applicationManager.GetIdAsync(application);
            var clientId = await applicationManager.GetClientIdAsync(application);

            // Create the claims-based identity for the client
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.AddClaim(OpenIddictConstants.Claims.Subject, applicationId ?? string.Empty);
            identity.AddClaim(OpenIddictConstants.Claims.Name, clientId ?? string.Empty);

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(GetDestinations);

            logger.ClientCredentialsValidated(clientId ?? string.Empty);
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
            logger.ProcessingPasswordGrant(request.Username ?? string.Empty);
            var user = await userManager.FindByNameAsync(request.Username ?? string.Empty);
            if (user == null)
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Validate the username/password
            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password ?? string.Empty, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                logger.PasswordSignInFailed(request.Username ?? string.Empty);
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Create the claims-based identity
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id);
            identity.AddClaim(OpenIddictConstants.Claims.Name, user.Username);

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(GetDestinations);

            logger.PasswordValidated(request.Username ?? string.Empty);
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            logger.ProcessingRefreshTokenGrant();
            var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            if (claimsPrincipal == null)
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var user = await userManager.FindByIdAsync(claimsPrincipal.GetClaim(OpenIddictConstants.Claims.Subject) ?? string.Empty);
            if (user == null)
            {
                logger.RefreshTokenUserNotFound();
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            if (!await signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(
                claimsPrincipal.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Subject
            => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
        _ => [OpenIddictConstants.Destinations.AccessToken]
    };
}
