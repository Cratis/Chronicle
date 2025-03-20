// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for using Cratis.Chronicle with a <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class ChronicleClientWebApplicationBuilderExtensions
{
    /// <summary>
    /// Gets the default section path for the Chronicle configuration.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Chronicle"];

    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to build on.</param>
    /// <param name="configureOptions">Optional <see cref="Action{T}"/> for configuring options.</param>
    /// <param name="configSection">Optional config section.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <param name="configureChronicleOptions">Optional <see cref="Action{T}"/> for configuring Chronicle options.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder AddCratisChronicle(
        this WebApplicationBuilder builder,
        Action<ChronicleAspNetCoreOptions>? configureOptions = default,
        string? configSection = default,
        ILoggerFactory? loggerFactory = default,
        Action<ChronicleOptions>? configureChronicleOptions = default)
    {
        builder.Services.AddOptions(configureOptions);
        builder.Configuration.Bind(configSection ?? ConfigurationPath.Combine(DefaultSectionPaths));

        builder.Services
            .AddRules()
            .AddUnitOfWork()
            .AddAggregates()
            .AddCompliance()
            .AddCausation()
            .AddCratisChronicleClient(configureChronicleOptions)
            .AddHostedService<ChronicleClientStartupTask>();
        builder.Host.AddCratisChronicle(loggerFactory);
        return builder;
    }

    /// <summary>
    /// Configures the usage of Cratis for the app.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisChronicle(this IApplicationBuilder app)
    {
        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        appLifetime.ApplicationStarted.Register(() => GlobalInstances.ServiceProvider = app.ApplicationServices);

        return app;
    }

    static OptionsBuilder<ChronicleAspNetCoreOptions> AddOptions(this IServiceCollection services, Action<ChronicleAspNetCoreOptions>? configure = default)
    {
        var builder = services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure != default)
        {
            builder.Configure(configure);
        }

        return builder;
    }
}
