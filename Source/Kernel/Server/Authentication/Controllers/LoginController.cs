// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication.Controllers;

/// <summary>
/// Controller for handling login requests.
/// </summary>
/// <param name="userManager">The user manager.</param>
/// <param name="signInManager">The sign-in manager.</param>
/// <param name="userStorage">The user storage.</param>
[ApiController]
[Route("api/security")]
[AllowAnonymous]
public class LoginController(
    UserManager<ChronicleUser> userManager,
    SignInManager<ChronicleUser> signInManager,
    IUserStorage userStorage) : ControllerBase
{
    /// <summary>
    /// Handles login requests.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <returns>The login response.</returns>
    [HttpPost("login")]
    [Produces("application/json")]
    [AspNetResult]
    public async Task<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
        }

        // Get the full user details to check RequiresPasswordChange
        var chronicleUser = await userStorage.GetById(user.Id);

        return new LoginResponse
        {
            Success = true,
            RequiresPasswordChange = chronicleUser?.RequiresPasswordChange ?? false,
            UserId = Guid.Parse(user.Id.ToString())
        };
    }
}
