// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Server.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Removes the controllers that sign callers in when authentication is disabled.
/// </summary>
/// <remarks>
/// The sign-in controllers are backed by ASP.NET Core Identity, which is not registered when authentication is
/// disabled. Left in place they are still routed, and activating them fails with an unresolvable
/// <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}"/> - an HTTP 500 for every client that asks
/// for a token. Removing them from the controller feature means their routes are never mapped, so those requests get a plain 404, the same answer
/// the Identity API endpoints already give in this mode because they are not mapped either. A client that holds
/// credentials then logs the rejected token request and continues without a token, which the kernel accepts
/// because nothing is authenticated.
/// </remarks>
public class AuthenticationDisabledControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    /// <summary>
    /// The controllers that require authentication to be enabled.
    /// </summary>
    public static readonly IReadOnlyCollection<Type> AuthenticationControllers =
    [
        typeof(AuthorizationController),
        typeof(LoginController)
    ];

    /// <inheritdoc/>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controller in feature.Controllers.Where(IsAuthenticationController).ToList())
        {
            feature.Controllers.Remove(controller);
        }
    }

    static bool IsAuthenticationController(TypeInfo controller) => AuthenticationControllers.Contains(controller.AsType());
}
