// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
    /// <param name="configure">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder AddCratisChronicle(
        this WebApplicationBuilder builder,
        Action<ChronicleAspNetCoreOptions>? configureOptions = default,
        string? configSection = default,
        Action<IChronicleBuilder>? configure = default,
        ILoggerFactory? loggerFactory = default)
    {
        ConceptTypeConvertersRegistrar.EnsureFor(typeof(ChronicleClientWebApplicationBuilderExtensions).Assembly);
        ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

        var configSectionPath = configSection ?? ConfigurationPath.Combine(DefaultSectionPaths);

        builder.Services.AddOptions(configureOptions, configSectionPath);

        builder.Services
            .AddUnitOfWork()
            .AddCompliance()
            .AddCausation()
            .AddCratisChronicleClient();

        var options = BuildChronicleOptions(builder.Configuration, configSectionPath, configureOptions);
        builder.Services.AddCratisChronicleArtifacts(options);

        var chronicleBuilder = new ChronicleBuilder(builder.Services, builder.Configuration, options.ArtifactsProvider);
        configure?.Invoke(chronicleBuilder);

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
        appLifetime.ApplicationStarted.Register(() =>
        {
            GlobalInstances.ServiceProvider = app.ApplicationServices;
            var client = app.ApplicationServices.GetRequiredService<IChronicleClient>();
            var options = app.ApplicationServices.GetService<IOptions<ChronicleAspNetCoreOptions>>()!;
            var eventStore = client.GetEventStore(options.Value.EventStore).GetAwaiter().GetResult();
            eventStore.Connection.Connect().Wait();
        });

        return app;
    }

    static OptionsBuilder<ChronicleAspNetCoreOptions> AddOptions(this IServiceCollection services, Action<ChronicleAspNetCoreOptions>? configure = default, string? configSectionPath = default)
    {
        var sectionPath = configSectionPath ?? ConfigurationPath.Combine(DefaultSectionPaths);

        var builder = services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .BindConfiguration(sectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var baseBuilder = services
            .AddOptions<ChronicleOptions>()
            .BindConfiguration(sectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            builder.Configure(configure);
            baseBuilder.Configure(options =>
            {
                var aspNetCoreOptions = new ChronicleAspNetCoreOptions();
                CopyValues(aspNetCoreOptions, options);
                configure(aspNetCoreOptions);
                CopyValues(options, aspNetCoreOptions);
            });
        }

        return builder;
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "IConfiguration is the appropriate abstraction for flexibility")]
    static ChronicleOptions BuildChronicleOptions(IConfiguration configuration, string configSectionPath, Action<ChronicleAspNetCoreOptions>? configure = default)
    {
        var options = new ChronicleOptions();
        configuration.GetSection(configSectionPath).Bind(options);

        if (configure is not null)
        {
            var aspNetCoreOptions = new ChronicleAspNetCoreOptions();
            configuration.GetSection(configSectionPath).Bind(aspNetCoreOptions);
            configure(aspNetCoreOptions);
            CopyValues(options, aspNetCoreOptions);
        }

        return options;
    }

    static void CopyValues(object target, object source)
    {
        foreach (var property in target.GetType().GetProperties())
        {
            var value = source.GetType().GetProperty(property.Name)?.GetValue(source);
            if (property?.CanWrite == true)
            {
                property.SetValue(target, value);
            }
        }
    }
}
