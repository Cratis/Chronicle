// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore;
using Cratis.Chronicle.AspNetCore.Auditing;
using Cratis.Chronicle.AspNetCore.Transactions;
using Cratis.Chronicle.Connections;
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
    /// <returns><see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder AddCratisChronicle(
        this WebApplicationBuilder builder,
        Action<ChronicleAspNetCoreOptions>? configureOptions = default,
        string? configSection = default,
        Action<IChronicleBuilder>? configure = default)
        => builder.AddCratisChronicle(configureOptions, configSection, configure, "chronicle");

    /// <summary>
    /// Configures the Chronicle client using a named connection string as the fallback endpoint.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to configure Chronicle on.</param>
    /// <param name="configureOptions">Optional callback whose connection string overrides configuration.</param>
    /// <param name="configSection">Optional configuration section path override. Defaults to <c language="csharp">Cratis:Chronicle</c>.</param>
    /// <param name="configure">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <param name="connectionStringName">Name under <c language="csharp">ConnectionStrings</c>, typically the Aspire resource name.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for configuration continuation.</returns>
    public static WebApplicationBuilder AddCratisChronicle(
        this WebApplicationBuilder builder,
        Action<ChronicleAspNetCoreOptions>? configureOptions,
        string? configSection,
        Action<IChronicleBuilder>? configure,
        string connectionStringName)
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
        builder.Services.AddChronicleOptions(configureOptions, configSectionPath, connectionStringName);

        builder.Services
            .AddUnitOfWork()
            .AddCompliance()
            .AddCausation();

        // Let the caller configure structural dependencies on the builder. This runs after
        // AddChronicleOptions so that the caller can also interact with registered services.
        configure?.Invoke(chronicleBuilder);

        builder.Services.AddCratisChronicleClient(chronicleBuilder);
        builder.Services.AddChronicleHealthCheck();

        return builder;
    }

    /// <summary>
    /// Configures Chronicle startup and, when automatic middleware is disabled, installs its middleware at this position.
    /// </summary>
    /// <remarks>Call after routing, authentication, and authorization when the namespace depends on the authenticated user.</remarks>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratisChronicle(this IApplicationBuilder app)
    {
        const string middlewareKey = "Cratis.Chronicle.MiddlewareRegistered";
        if (app.Properties.ContainsKey(middlewareKey))
        {
            return app;
        }
        app.Properties[middlewareKey] = true;

        if (!app.ApplicationServices.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value.UseAutomaticMiddleware)
        {
            app.UseMiddleware<UnitOfWorkMiddleware>();
            app.UseMiddleware<CausationMiddleware>();
        }

        GlobalInstances.ServiceProvider = app.ApplicationServices;
        var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        appLifetime.ApplicationStarted.Register(() =>
        {
            var client = app.ApplicationServices.GetRequiredService<IChronicleClient>();
            var options = app.ApplicationServices.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<IApplicationBuilder>>();

            try
            {
                var eventStore = client.GetEventStore(options.Value.EventStore).GetAwaiter().GetResult();
                eventStore.Connection.Connect().GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is ConnectionTimedOut or ConnectionUnavailable)
            {
                // A kernel that is briefly unreachable while a host starts is ordinary, and the connection's
                // watchdog keeps reconnecting in the background - so this must not take the host down with it.
                // Connecting now reports failure rather than swallowing it (#3948), which is why this is caught
                // here rather than never raised. The Chronicle health check reports the state until it recovers.
                //
                // Only the connection failures are caught. Anything else here means the client is misconfigured
                // rather than unable to reach a kernel - it will never come good on its own, so it keeps taking
                // startup down where it is seen instead of leaving a host running with no event store.
                logger.CouldNotConnectOnStartup(ex);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers <see cref="ChronicleAspNetCoreOptions"/> and <see cref="ChronicleOptions"/> in the options pipeline.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Optional user callback for configuring options.</param>
    /// <param name="configSectionPath">Configuration section path to bind.</param>
    /// <param name="connectionStringName">Name of the fallback connection string.</param>
    static void AddChronicleOptions(
        this IServiceCollection services,
        Action<ChronicleAspNetCoreOptions>? configure,
        string configSectionPath,
        string connectionStringName)
    {
        services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .WithChronicleConfiguration(configSectionPath, connectionStringName)
            .Configure(options => configure?.Invoke(options))
            .Configure<IOptions<ChronicleClientOptions>>((options, clientOptions) =>
            {
                if (string.IsNullOrWhiteSpace(options.EventStore.Value))
                {
                    options.EventStore = clientOptions.Value.EventStore;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var baseBuilder = services
            .AddOptions<ChronicleOptions>()
            .WithChronicleConfiguration(configSectionPath, connectionStringName)
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
