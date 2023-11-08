// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Aksio.Cratis.AspNetCore;
using Aksio.Cratis.Client;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Reducers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for using Aksio.Cratis with a <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="IClientBuilder"/> for a non-microservice oriented scenario.
    /// </summary>
    /// <param name="webApplicationBuilder"><see cref="WebApplicationBuilder"/> to build on.</param>
    /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder UseCratis(
        this WebApplicationBuilder webApplicationBuilder,
        Action<IClientBuilder>? configureDelegate = default)
    {
        webApplicationBuilder.Services.AddRules();
        webApplicationBuilder.Services.AddHttpContextAccessor();
        webApplicationBuilder.Host.UseCratis(configureDelegate);
        return webApplicationBuilder;
    }

    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <param name="automaticallyConnect">Whether or not to automatically connect to Cratis or not.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratis(this IApplicationBuilder app, bool automaticallyConnect = true)
    {
        app.UseCausation();
        app.UseExecutionContext();

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints
            .MapClientObservers()
            .MapClientReducers());

        if (automaticallyConnect)
        {
            var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            appLifetime.ApplicationStarted.Register(() =>
            {
                GlobalInstances.ServiceProvider = app.ApplicationServices;
                app.ApplicationServices.GetRequiredService<IClient>().Connect().Wait();
            });
        }

        return app;
    }
}
