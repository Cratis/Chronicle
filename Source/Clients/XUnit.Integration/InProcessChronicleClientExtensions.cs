// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.AspNetCore.Identities;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Identities;
using Cratis.Serialization;
using Cratis.Traces;
using KernelCore::Cratis.Chronicle.Observation.Reactors.Clients;
using KernelCore::Cratis.Chronicle.Observation.Reducers.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Provides registration of an in-process Chronicle client co-hosted on an Orleans silo.
/// </summary>
/// <remarks>
/// The in-process client talks to grains through the silo's local <see cref="IGrainFactory"/>, which means
/// it participates in the same cluster as the silo. This is the mechanism used by integration tests that
/// need an <see cref="IEventStore"/> against a real (in-process) Chronicle silo without going over gRPC.
/// </remarks>
public static class InProcessChronicleClientExtensions
{
    /// <summary>
    /// Adds an in-process Chronicle client to the silo's services so that <see cref="IEventStore"/> and its
    /// sub-services can be resolved from the silo's service provider.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the client to.</param>
    /// <param name="artifactsProvider">The <see cref="IClientArtifactsProvider"/> used to discover event types, reactors, reducers and projections.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the client connects to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddInProcessChronicleClient(
        this IServiceCollection services,
        IClientArtifactsProvider artifactsProvider,
        EventStoreName eventStore)
    {
        services.AddSingleton<IReactorMediator, ReactorMediator>();
        services.AddSingleton<IReducerMediator, ReducerMediator>();

        services.RemoveAll<IClientArtifactsProvider>();
        services.AddSingleton(artifactsProvider);

        services.PostConfigure<ChronicleClientOptions>(options => options.EventStore = eventStore);

        services.AddSingleton<INamingPolicy>(new DefaultNamingPolicy());
        services.AddHttpContextAccessor();
        services.AddSingleton<IIdentityProvider>(sp => new IdentityProvider(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<ILogger<IdentityProvider>>()));

        // Do NOT register Globals.JsonSerializerOptions here. The silo's ConfigureSerialization
        // already registers a JsonSerializerOptions configured with all the Chronicle converters
        // (concepts, enums, ExpandoObject, etc.). The custom Orleans codecs (AppendedEventSerializer,
        // ExpandoObjectSerializer, ...) resolve JsonSerializerOptions from DI. Overriding it with the
        // bare client options would make this silo serialize Cratis types differently from every other
        // silo in the cluster, corrupting the wire stream on cross-silo grain calls.
        services.AddNamedActivitySource(ClientActivity.SourceName);

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(IActivitySource<>) &&
                Equals(descriptor.ServiceKey, ClientActivity.SourceName))
            {
                services.RemoveAt(index);
                break;
            }
        }

        services.AddKeyedSingleton<IActivitySource<EventSequences.EventSequence>>(ClientActivity.SourceName, (sp, key) =>
            new ActivitySource<EventSequences.EventSequence>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));
        services.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (sp, key) =>
            new ActivitySource<Reactors.Reactors>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));
        services.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (sp, key) =>
            new ActivitySource<Reducers.Reducers>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));

        services.AddSingleton<IChronicleClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
            var provider = sp.GetRequiredService<IClientArtifactsProvider>();
            var identityProvider = sp.GetRequiredService<IIdentityProvider>();
            var grainFactory = sp.GetRequiredService<IGrainFactory>();
            var chronicleServices = sp.GetRequiredService<IServices>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var connectionLifecycle = new ConnectionLifecycle(loggerFactory.CreateLogger<ConnectionLifecycle>());
            var connection = new ChronicleConnection(connectionLifecycle, grainFactory, loggerFactory);
            connection.SetServices(chronicleServices);

            return new ChronicleClient(connection, options, provider, sp, identityProvider, loggerFactory: loggerFactory);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IChronicleClient>();
            return client.GetEventStore(eventStore).GetAwaiter().GetResult();
        });

        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Connection);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventTypes);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventLog);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reactors);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reducers);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Projections);
        services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().ReadModels);

        return services;
    }
}
