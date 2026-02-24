// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.AspNetCore.Identities;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.InProcess;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ChronicleOptions = Cratis.Chronicle.ChronicleOptions;

namespace Orleans.Hosting;

/// <summary>
/// Extensions for <see cref="ISiloBuilder"/> to use with current silo.
/// </summary>
public static class ChronicleClientSiloBuilderExtensions
{
    /// <summary>
    /// Gets the default section path for the Chronicle configuration.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Chronicle"];

#if NET8_0
    static readonly object _eventStoreInitLock = new();
#else
    static readonly Lock _eventStoreInitLock = new();
#endif

    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="configureChronicle">Optional delegate for configuring the <see cref="IChronicleInProcessBuilder"/>.</param>
    /// <param name="configSection">Optional config section.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddCratisChronicle(
        this ISiloBuilder builder,
        Action<IChronicleInProcessBuilder>? configureChronicle = default,
        string? configSection = default)
    {
        builder.ConfigureServices(services => AddOptions(services)
                .BindConfiguration(configSection ?? ConfigurationPath.Combine(DefaultSectionPaths)));

        ConfigureChronicle(builder, configureChronicle);

        return builder;
    }

    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="configureOptions">Callback for providing options.</param>
    /// <param name="configureChronicle">Optional delegate for configuring the <see cref="IChronicleInProcessBuilder"/>.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddCratisChronicle(
        this ISiloBuilder builder,
        Action<ChronicleOrleansInProcessOptions> configureOptions,
        Action<IChronicleInProcessBuilder>? configureChronicle = default)
    {
        builder.ConfigureServices(services => AddOptions(services, configureOptions));
        ConfigureChronicle(builder, configureChronicle);

        return builder;
    }

    static OptionsBuilder<ChronicleOrleansInProcessOptions> AddOptions(this IServiceCollection services, Action<ChronicleOrleansInProcessOptions>? configure = default)
    {
        var builder = services
            .AddOptions<ChronicleOrleansInProcessOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        return builder;
    }

    static void ConfigureChronicle(this ISiloBuilder builder, Action<IChronicleInProcessBuilder>? configureChronicle = default)
    {
        ConceptTypeConvertersRegistrar.EnsureFor(typeof(ChronicleClientSiloBuilderExtensions).Assembly);
        ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

        // Add Chronicle to the silo as the first thing we do, order matters - this is especially important for the different call filters.
        builder.AddChronicleToSilo(
            configureChronicle is not null
                ? coreBuilder => configureChronicle(new ChronicleInProcessBuilder(coreBuilder))
                : null);
        builder.AddActivityPropagation();
        builder.ConfigureServices(services =>
        {
            services.AddTypeDiscovery();
            services.AddBindingsByConvention();
            services.AddSelfBindings();

            services.AddSingleton<IReactorMediator, ReactorMediator>();
            services.AddSingleton<IReducerMediator, ReducerMediator>();
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.ArtifactsProvider);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.NamingPolicy);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.IdentityProvider);
            services.AddSingleton(Globals.JsonSerializerOptions);
            services.AddHttpContextAccessor();
            services.AddSingleton<IChronicleClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
                options.ServiceProvider = sp;
                options.ArtifactsProvider = sp.GetRequiredService<IClientArtifactsProvider>();
                options.NamingPolicy = sp.GetRequiredService<INamingPolicy>();
                options.IdentityProvider = new IdentityProvider(
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<ILogger<IdentityProvider>>());

                var grainFactory = sp.GetRequiredService<IGrainFactory>();
                var services = sp.GetRequiredService<IServices>();

                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var connectionLifecycle = new ConnectionLifecycle(loggerFactory.CreateLogger<ConnectionLifecycle>());
                var connection = new Cratis.Chronicle.InProcess.ChronicleConnection(connectionLifecycle, grainFactory, loggerFactory);
                connection.SetServices(services);
                return new ChronicleClient(connection, options);
            });

            services.AddSingleton(sp =>
            {
                lock (_eventStoreInitLock)
                {
                    var options = sp.GetRequiredService<IOptions<ChronicleOrleansInProcessOptions>>().Value;
                    var client = sp.GetRequiredService<IChronicleClient>();
                    return client.GetEventStore(options.EventStoreName).GetAwaiter().GetResult();
                }
            });

            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Connection);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().UnitOfWorkManager);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventTypes);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventLog);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reactors);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reducers);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Projections);
        });
    }
}
