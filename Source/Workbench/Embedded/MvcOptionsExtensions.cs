// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Holds extension methods for configuring route prefixes for API controllers.
/// </summary>
public static class MvcOptionsExtensions
{
    /// <summary>
    /// Use the route prefix specified by the <see cref="IRouteTemplateProvider"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/> to add prefix to.</param>
    /// <param name="routeAttribute"><see cref="IRouteTemplateProvider"/> representing the prefix.</param>
    public static void UseRoutePrefix(this MvcOptions options, IRouteTemplateProvider routeAttribute)
    {
        options.Conventions.Add(new RoutePrefixConvention(routeAttribute));
    }

    /// <summary>
    /// Use the route prefix specified by the prefix.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/> to add prefix to.</param>
    /// <param name="prefix">The prefix to use.</param>
    public static void UseRoutePrefix(this MvcOptions options, string prefix)
    {
        if (prefix.StartsWith('/')) prefix = prefix[1..];
        if (!prefix.EndsWith('/')) prefix = $"{prefix}/";
        options.UseRoutePrefix(new RouteAttribute(prefix));
    }
}
