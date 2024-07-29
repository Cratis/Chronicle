// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for using Cratis.Chronicle with a <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="webApplicationBuilder"><see cref="WebApplicationBuilder"/> to build on.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder UseCratisChronicle(
        this WebApplicationBuilder webApplicationBuilder,
        ILoggerFactory? loggerFactory = default)
    {
        webApplicationBuilder.Services.AddRules();
        webApplicationBuilder.Services.AddHttpContextAccessor();
        webApplicationBuilder.Host.UseCratisChronicle(loggerFactory);
        return webApplicationBuilder;
    }

    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisChronicle(this IApplicationBuilder app)
    {
        app.UseCausation();

        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        appLifetime.ApplicationStarted.Register(() => GlobalInstances.ServiceProvider = app.ApplicationServices);

        return app;
    }
}
