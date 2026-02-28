// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using Microsoft.AspNetCore.Mvc.Controllers;

namespace Cratis.Chronicle.Api;

/// <summary>
/// Represents a custom controller activator for creating controllers in the Cratis Chronicle API.
/// </summary>
public class CustomControllerActivator : IControllerActivator
{
    /// <inheritdoc/>
    public object Create(ControllerContext context)
    {
        var type = context.ActionDescriptor.ControllerTypeInfo.AsType();
        try
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(context.HttpContext.RequestServices, type);
        }
        catch
        {
            // Fallback for internal constructors
            var constructors = type.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var ctor = constructors
                .OrderByDescending(c => c.IsPublic)
                .ThenByDescending(c => c.IsFamily || c.IsAssembly || c.IsFamilyOrAssembly)
                .FirstOrDefault() ??
                throw new InvalidOperationException($"No suitable constructor found for controller type '{type.FullName}'.");

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            var provider = context.HttpContext.RequestServices;
            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = provider.GetService(parameters[i].ParameterType);
            }
            return ctor.Invoke(args);
        }
    }

    /// <inheritdoc/>
    public void Release(ControllerContext context, object controller)
    {
    }
}
