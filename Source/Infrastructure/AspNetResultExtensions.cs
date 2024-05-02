// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Extensions for <see cref="ActionExecutingContext"/>.
/// </summary>
public static class AspNetResultExtensions
{
    /// <summary>
    /// Check if an action has the with <see cref="AspNetResultAttribute"/> as filter.
    /// </summary>
    /// <param name="context"><see cref="ActionExecutingContext"/> to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool IsAspNetResult(this ActionExecutingContext context)
        => context.Filters.Any(_ => _ is AspNetResultAttribute);
}
