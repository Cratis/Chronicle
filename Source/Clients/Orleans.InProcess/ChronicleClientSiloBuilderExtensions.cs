// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Grains.Observation.Reactors.Clients;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Orleans.InProcess;
using Cratis.Chronicle.Orleans.Transactions;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="configureChronicle">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <param name="configSection">Optional config section.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddCratisChronicle(
        this ISiloBuilder builder,
        Action<IChronicleBuilder>? configureChronicle = default,
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
    /// <param name="configureChronicle">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddCratisChronicle(
        this ISiloBuilder builder,
        Action<ChronicleOrleansInProcessOptions> configureOptions,
        Action<IChronicleBuilder>? configureChronicle = default)
    {
        // We disable the AspNet client registration.
        // The AspNetCore client uses an `IHostedService` to register the client, which then runs before the Silo is ready.
        // Leading to crashing the entire process.
        ChronicleClientStartupTask.RegistrationEnabled = false;

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

    static void ConfigureChronicle(this ISiloBuilder builder, Action<IChronicleBuilder>? configureChronicle = default)
    {
        builder.AddIncomingGrainCallFilter<UnitOfWorkIncomingCallFilter>();
        builder.AddOutgoingGrainCallFilter<UnitOfWorkOutgoingCallFilter>();
        builder.AddChronicleToSilo(configureChronicle);
        builder.AddStartupTask<ChronicleOrleansClientStartupTask>();
        builder.ConfigureServices(services =>
        {
            services.AddTypeDiscovery();
            services.AddBindingsByConvention();
            services.AddSelfBindings();

            services.AddSingleton<IReactorMediator, ReactorMediator>();
            services.AddSingleton<IReducerMediator, ReducerMediator>();
            services.AddSingleton<IRules, Rules>();

            services.AddSingleton<IClientArtifactsProvider>(sp => new DefaultOrleansClientArtifactsProvider(sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.ArtifactsProvider));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.ModelNameConvention);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.IdentityProvider);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.JsonSerializerOptions);

            services.AddSingleton<IChronicleClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
                options.ServiceProvider = sp;
                options.ArtifactsProvider = sp.GetRequiredService<IClientArtifactsProvider>();

                var grainFactory = sp.GetRequiredService<IGrainFactory>();
                var services = sp.GetRequiredService<IServices>();

                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                var connectionLifecycle = new ConnectionLifecycle(loggerFactory.CreateLogger<ConnectionLifecycle>());
                var connection = new Cratis.Chronicle.Orleans.InProcess.ChronicleConnection(connectionLifecycle, grainFactory, loggerFactory);
                connection.SetServices(services);
                return new ChronicleClient(connection, options);
            });

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ChronicleOrleansInProcessOptions>>().Value;
                var client = sp.GetRequiredService<IChronicleClient>();
                return client.GetEventStore(options.EventStoreName);
            });

            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Connection);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().UnitOfWorkManager);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().AggregateRootFactory);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventTypes);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventLog);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reactors);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reducers);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Projections);
        });
    }
}
