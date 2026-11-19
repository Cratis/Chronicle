// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Server.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Leaves the Workbench's password sign-in controller out of MVC when a trusted proxy authenticates HTTP callers.
/// The token endpoint stays, because gRPC clients still exchange their client credentials there.
/// </summary>
public class TrustedProxyControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    /// <summary>
    /// The controllers left out.
    /// </summary>
    public static readonly IReadOnlyCollection<Type> PasswordSignInControllers = [typeof(LoginController)];

    /// <inheritdoc/>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controller in feature.Controllers.Where(IsPasswordSignInController).ToList())
        {
            feature.Controllers.Remove(controller);
        }
    }

    static bool IsPasswordSignInController(TypeInfo controller) => PasswordSignInControllers.Contains(controller.AsType());
}
