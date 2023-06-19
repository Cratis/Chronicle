// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents extension methods for building on <see cref="IApplicationBuilder"/>.
/// </summary>
public static class BootApplicationBuilderExtensions
{
    static bool _bootProceduresPerformed;

    /// <summary>
    /// Perform all <see cref="IPerformBootProcedure">boot procedures</see>.
    /// </summary>
    /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/> to add to.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder PerformBootProcedures(this IApplicationBuilder applicationBuilder)
    {
        if (_bootProceduresPerformed) return applicationBuilder;
        applicationBuilder.ApplicationServices.GetService<IBootProcedures>()!.Perform();
        _bootProceduresPerformed = true;
        return applicationBuilder;
    }
}
