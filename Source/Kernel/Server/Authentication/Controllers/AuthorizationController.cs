// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Storage.ClientCredentials;
using Cratis.Chronicle.Storage.Users;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
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
/// <param name="clientCredentialsStorage">The client credentials storage.</param>
[ApiController]
[Route("connect")]
public class AuthorizationController(
    UserManager<ChronicleUser> userManager,
    SignInManager<ChronicleUser> signInManager,
    IClientCredentialsStorage clientCredentialsStorage) : ControllerBase
{
    /// <summary>
    /// Handles token requests.
    /// </summary>
    /// <returns>The token response.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the OpenID Connect request cannot be retrieved or the grant type is not supported.</exception>
    [HttpPost("token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsClientCredentialsGrantType())
        {
            // In client_credentials flow, the client_id and client_secret are the credentials
            var clientId = request.ClientId ?? string.Empty;
            var clientSecret = request.ClientSecret ?? string.Empty;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client credentials are invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var client = await clientCredentialsStorage.GetByClientId(clientId);
            if (client?.IsActive != true)
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client credentials are invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Verify the client secret
            if (!VerifySecret(clientSecret, client.ClientSecret))
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client credentials are invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Create the claims-based identity for the client
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            identity.AddClaim(OpenIddictConstants.Claims.Subject, client.Id);
            identity.AddClaim(OpenIddictConstants.Claims.Name, client.ClientId);

            identity.SetScopes(request.GetScopes());
            identity.SetDestinations(GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
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

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
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

    static bool VerifySecret(string secret, string hashedSecret)
    {
        var parts = hashedSecret.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var testHash = KeyDerivation.Pbkdf2(
            password: secret,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return hash.SequenceEqual(testHash);
    }

    static IEnumerable<string> GetDestinations(Claim claim) => claim.Type switch
    {
        OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Subject
            => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
        _ => [OpenIddictConstants.Destinations.AccessToken]
    };
}
