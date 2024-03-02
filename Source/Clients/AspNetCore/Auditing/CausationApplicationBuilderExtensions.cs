// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.AspNetCore.Auditing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for setting up causation.
/// </summary>
public static class CausationApplicationBuilderExtensions
{
    /// <summary>
    /// Use causation.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCausation(this IApplicationBuilder app)
    {
        app.UseMiddleware<CausationMiddleware>();
        return app;
    }
}
