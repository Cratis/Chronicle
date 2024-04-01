// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.ProxyGenerator.Templates;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="MethodInfo"/>.
/// </summary>
public static class MethodInfoExtensions
{
    /// <summary>
    /// Get the route for a method.
    /// </summary>
    /// <param name="method">Method to get for.</param>
    /// <returns>The full route.</returns>
    public static string GetRoute(this MethodInfo method)
    {
        var routeTemplates = new string[]
        {
            method.DeclaringType?.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty,
            method.GetCustomAttribute<HttpGetAttribute>()?.Template ?? string.Empty,
            method.GetCustomAttribute<HttpGetAttribute>()?.Template ?? string.Empty
        };

        var route = string.Empty;

        foreach (var template in routeTemplates)
        {
            route = $"{route}/{template}".Trim('/');
        }

        if (!route.StartsWith('/')) route = $"/{route}";
        return route;
    }

    /// <summary>
    /// Get argument descriptors for a method.
    /// </summary>
    /// <param name="methodInfo">Method to get for.</param>
    /// <returns>Collection of <see cref="RequestArgumentDescriptor"/>. </returns>
    public static IEnumerable<RequestArgumentDescriptor> GetArgumentDescriptors(this MethodInfo methodInfo) =>
        methodInfo.GetParameters().Where(_ => _.IsRequestArgument()).Select(_ => _.ToRequestArgumentDescriptor());

    /// <summary>
    /// Check if a method is a query method.
    /// </summary>
    /// <param name="method">Method to check.</param>
    /// <returns>True if it is a query method, false otherwise.</returns>
    public static bool IsQueryMethod(this MethodInfo method) =>
         method.GetCustomAttribute<HttpGetAttribute>() != null &&
         method.GetCustomAttribute<AspNetResultAttribute>() == null;

    /// <summary>
    /// Check if a method is a query method.
    /// </summary>
    /// <param name="method">Method to check.</param>
    /// <returns>True if it is a query method, false otherwise.</returns>
    public static bool IsCommandMethod(this MethodInfo method) =>
         method.GetCustomAttribute<HttpPostAttribute>() != null &&
         method.GetCustomAttribute<AspNetResultAttribute>() == null;
}
