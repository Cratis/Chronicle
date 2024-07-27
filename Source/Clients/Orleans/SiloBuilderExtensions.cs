// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains.Observation.Reactions.Clients;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Orleans;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server::Cratis.Chronicle.Services.Events;
using Server::Cratis.Chronicle.Services.EventSequences;
using Server::Cratis.Chronicle.Services.Observation;

namespace Orleans.Hosting;

/// <summary>
/// Extensions for <see cref="ISiloBuilder"/> to use with current silo.
/// </summary>
public static class SiloBuilderExtensions
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
    public static ISiloBuilder AddChronicle(this ISiloBuilder builder, Action<IChronicleBuilder>? configureChronicle = default, string? configSection = default)
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
    public static ISiloBuilder AddChronicle(this ISiloBuilder builder, Action<ChronicleOptions> configureOptions, Action<IChronicleBuilder>? configureChronicle = default)
    {
        builder.ConfigureServices(services => AddOptions(services, configureOptions));
        ConfigureChronicle(builder, configureChronicle);

        return builder;
    }

    static OptionsBuilder<ChronicleOptions> AddOptions(this IServiceCollection services, Action<ChronicleOptions>? configure = default)
    {
        var builder = services
            .AddOptions<ChronicleOptions>()
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
        builder.AddChronicleToSilo(configureChronicle);
        builder.AddStartupTask<ChronicleStartupTask>();
        builder.ConfigureServices(services =>
        {
            services.AddTypeDiscovery();
            services.AddBindingsByConvention();
            services.AddSelfBindings();

            services.AddSingleton<IReactionMediator, ReactionMediator>();
            services.AddSingleton<IReducerMediator, ReducerMediator>();

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.ArtifactsProvider);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.ModelNameConvention);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.IdentityProvider);
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChronicleOptions>>().Value.JsonSerializerOptions);

            services.AddSingleton<IChronicleClient>(sp =>
            {
                var grainFactory = sp.GetRequiredService<IGrainFactory>();
                var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
                var storage = sp.GetRequiredService<IStorage>();
                var services = new Cratis.Chronicle.Services(
                    new EventSequences(grainFactory, storage, Globals.JsonSerializerOptions),
                    new EventTypes(storage),
                    new Observers(),
                    new Server::Cratis.Chronicle.Services.Observation.Reactions.Reactions(grainFactory, sp.GetRequiredService<IReactionMediator>()),
                    new Server::Cratis.Chronicle.Services.Observation.Reducers.Reducers(grainFactory, sp.GetRequiredService<IReducerMediator>(), sp.GetRequiredService<IExpandoObjectConverter>()),
                    new Server::Cratis.Chronicle.Services.Projections.Projections(grainFactory));

                var connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
                var connection = new Cratis.Chronicle.Orleans.ChronicleConnection(connectionLifecycle, services, grainFactory);
                options.ArtifactsProvider = new DefaultOrleansClientArtifactsProvider(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));
                return new ChronicleClient(connection, options);
            });

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
                var client = sp.GetRequiredService<IChronicleClient>();
                return client.GetEventStore("some_event_store");
            });

            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Connection);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().AggregateRootFactory);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventTypes);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventLog);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reactions);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reducers);
            services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Projections);
        });
    }
}
