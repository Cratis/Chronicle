// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up logging for an application.
/// </summary>
public static class LoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Use the default logging setup.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to use it with.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseDefaultLogging(this IApplicationBuilder app)
    {
        app.UseHttpLogging();

        return app;
    }
}
