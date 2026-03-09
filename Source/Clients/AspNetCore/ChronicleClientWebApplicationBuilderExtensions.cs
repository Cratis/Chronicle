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

        // Create the builder first with defaults. The builder collects structural dependencies
        // (artifacts provider, identity provider, etc.) before the DI container is built.
        // This is the same pattern as AddDbContext: structural dependencies are captured eagerly
        // at registration time and cannot be overridden via configuration.
        var chronicleBuilder = new ChronicleBuilder(builder.Services, builder.Configuration, DefaultClientArtifactsProvider.Default);

        // Register via the standard options pipeline for runtime resolution.
        builder.Services.AddChronicleOptions(configureOptions, configSectionPath);

        builder.Services
            .AddUnitOfWork()
            .AddCompliance()
            .AddCausation();

        // Let the caller configure structural dependencies on the builder. This runs after
        // AddChronicleOptions so that the caller can also interact with registered services.
        configure?.Invoke(chronicleBuilder);

        builder.Services.AddCratisChronicleClient(chronicleBuilder);

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
        GlobalInstances.ServiceProvider = app.ApplicationServices;
        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        appLifetime.ApplicationStarted.Register(() =>
        {
            var client = app.ApplicationServices.GetRequiredService<IChronicleClient>();
            var options = app.ApplicationServices.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>();
            var eventStore = client.GetEventStore(options.Value.EventStore).GetAwaiter().GetResult();
            eventStore.Connection.Connect().GetAwaiter().GetResult();
        });

        return app;
    }

    /// <summary>
    /// Registers <see cref="ChronicleAspNetCoreOptions"/> and <see cref="ChronicleOptions"/> in the options pipeline.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Optional user callback for configuring options.</param>
    /// <param name="configSectionPath">Configuration section path to bind.</param>
    static void AddChronicleOptions(
        this IServiceCollection services,
        Action<ChronicleAspNetCoreOptions>? configure,
        string configSectionPath)
    {
        services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .BindConfiguration(configSectionPath)
            .Configure(options => configure?.Invoke(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var baseBuilder = services
            .AddOptions<ChronicleOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            baseBuilder.Configure(options =>
            {
                var aspNetCoreOptions = new ChronicleAspNetCoreOptions();
                CopyValues(aspNetCoreOptions, options);
                configure(aspNetCoreOptions);
                CopyValues(options, aspNetCoreOptions);
            });
        }
    }

    static void CopyValues(object target, object source)
    {
        foreach (var property in target.GetType().GetProperties())
        {
            var value = source.GetType().GetProperty(property.Name)?.GetValue(source);
            if (property.CanWrite)
            {
                property.SetValue(target, value);
            }
        }
    }
}
