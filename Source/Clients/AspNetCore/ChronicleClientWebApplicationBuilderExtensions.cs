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

        builder.Services
            .AddOptions(configureOptions)
            .BindConfiguration(configSection ?? ConfigurationPath.Combine(DefaultSectionPaths));

        builder.Services
            .AddUnitOfWork()
            .AddCompliance()
            .AddCausation()
            .AddCratisChronicleClient();

        var options = new ChronicleOptions();
        builder.Configuration.GetSection(configSection ?? ConfigurationPath.Combine(DefaultSectionPaths)).Bind(options);

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

    static OptionsBuilder<ChronicleAspNetCoreOptions> AddOptions(this IServiceCollection services, Action<ChronicleAspNetCoreOptions>? configure = default)
    {
        var builder = services
            .AddOptions<ChronicleAspNetCoreOptions>()
            .BindConfiguration(ConfigurationPath.Combine(DefaultSectionPaths))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var baseBuilder = services
            .AddOptions<ChronicleOptions>()
            .BindConfiguration(ConfigurationPath.Combine(DefaultSectionPaths))
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
