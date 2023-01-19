// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Extension methods for application builder related to the Cratis client.
/// </summary>
public static class ClientApplicationBuilderExtensions
{
    /// <summary>
    /// Add Cratis client artifacts.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder AddCratisClient(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/.cratis/client/ping", ctx => Task.CompletedTask);
        });

        return app;
    }
}
