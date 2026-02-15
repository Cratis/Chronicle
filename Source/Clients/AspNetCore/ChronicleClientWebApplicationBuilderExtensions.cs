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

        // Determine the ArtifactsProvider eagerly by applying config binding + user callback.
        // Artifact types must be registered in DI before the container is built, so the provider
        // must be known at this point. This is the same pattern as AddDbContext (where the DB
        // provider must be set in the callback — you can't switch it via post-configuration).
        // All other options (EventStore, ConnectionString, etc.) remain fully configurable
        // through the standard options pipeline after this call.
        var artifactsProvider = ResolveArtifactsProvider(builder.Configuration, configSectionPath, configureOptions);

        // Register via the standard options pipeline for runtime resolution.
        builder.Services.AddChronicleOptions(configureOptions, configSectionPath, artifactsProvider);

        // Register artifact types into DI using the resolved provider.
        builder.Services.AddCratisChronicleArtifacts(artifactsProvider);

        builder.Services
            .AddUnitOfWork()
            .AddCompliance()
            .AddCausation()
            .AddCratisChronicleClient();

        var chronicleBuilder = new ChronicleBuilder(builder.Services, builder.Configuration, artifactsProvider);
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
    /// Resolves the <see cref="IClientArtifactsProvider"/> by binding configuration and applying the user callback.
    /// The provider must be determined at registration time because artifact types are registered in DI
    /// before the container is built.
    /// </summary>
    /// <param name="configuration"><see cref="ConfigurationManager"/> to bind from.</param>
    /// <param name="configSectionPath">Configuration section path to bind.</param>
    /// <param name="configureOptions">Optional user callback for configuring options.</param>
    /// <returns>The resolved <see cref="IClientArtifactsProvider"/>.</returns>
    static IClientArtifactsProvider ResolveArtifactsProvider(
        ConfigurationManager configuration,
        string configSectionPath,
        Action<ChronicleAspNetCoreOptions>? configureOptions)
    {
        var options = new ChronicleAspNetCoreOptions();
        configuration.GetSection(configSectionPath).Bind(options);
        configureOptions?.Invoke(options);
        return options.ArtifactsProvider;
    }

    /// <summary>
    /// Registers <see cref="ChronicleAspNetCoreOptions"/> and <see cref="ChronicleOptions"/> in the options pipeline.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Optional user callback for configuring options.</param>
    /// <param name="configSectionPath">Configuration section path to bind.</param>
    /// <param name="artifactsProvider">The <see cref="IClientArtifactsProvider"/> resolved at registration time.</param>
    static void AddChronicleOptions(
        this IServiceCollection services,
        Action<ChronicleAspNetCoreOptions>? configure,
        string configSectionPath,
        IClientArtifactsProvider artifactsProvider)
    {
        services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .BindConfiguration(configSectionPath)
            .Configure(options =>
            {
                configure?.Invoke(options);

                // Ensure the options always use the same provider that was used for DI registration.
                options.ArtifactsProvider = artifactsProvider;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var baseBuilder = services
            .AddOptions<ChronicleOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        baseBuilder.Configure(options => options.ArtifactsProvider = artifactsProvider);

        if (configure is not null)
        {
            baseBuilder.Configure(options =>
            {
                var aspNetCoreOptions = new ChronicleAspNetCoreOptions();
                CopyValues(aspNetCoreOptions, options);
                configure(aspNetCoreOptions);
                CopyValues(options, aspNetCoreOptions);
                options.ArtifactsProvider = artifactsProvider;
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
