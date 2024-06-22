// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;
extern alias Client;

using Client::Cratis.Chronicle;
using Client::Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains.Observation.Clients;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Json;
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
    /// <param name="configSection">Optional config section.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicle(this ISiloBuilder builder, string? configSection = default)
    {
        builder.ConfigureServices(services => AddOptions(services)
                .BindConfiguration(configSection ?? ConfigurationPath.Combine(DefaultSectionPaths)));

        ConfigureChronicle(builder, new ChronicleOptions());

        return builder;
    }

    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="configureOptions">Callback for providing options.</param>
    /// <returns>The <see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicle(this ISiloBuilder builder, Action<ChronicleOptions> configureOptions)
    {
        builder.ConfigureServices(services => AddOptions(services, configureOptions));
        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<ChronicleOptions>>();
        ConfigureChronicle(builder, options.Value);

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

    static void ConfigureChronicle(this ISiloBuilder builder, ChronicleOptions options)
    {
        builder.AddChronicleToSilo();
        builder.ConfigureServices(services =>
        {
            services.AddTypeDiscovery();
            services.AddBindingsByConvention();
            services.AddSelfBindings();

            services.AddSingleton<IObserverMediator, ObserverMediator>();

            services.AddSingleton<IChronicleClient>(sp =>
            {
                var services = new Client::Cratis.Chronicle.Services(
                    new EventSequences(sp.GetRequiredService<IGrainFactory>(), Globals.JsonSerializerOptions),
                    new EventTypes(sp.GetRequiredService<IStorage>()),
                    new Observers(),
                    new Server::Cratis.Chronicle.Services.Observation.ClientObservers(sp.GetRequiredService<IGrainFactory>(), sp.GetRequiredService<IObserverMediator>()));

                var connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
                var connection = new ChronicleConnection(connectionLifecycle, services);
                var client = new ChronicleClient(connection, options);
                var eventStore = client.GetEventStore("some_event_store");
                eventStore.DiscoverAll().GetAwaiter().GetResult();
                eventStore.RegisterAll().GetAwaiter().GetResult();
                return client;
            });
        });
    }
}
