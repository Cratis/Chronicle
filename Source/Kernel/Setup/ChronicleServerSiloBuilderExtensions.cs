// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Placement;
using Cratis.Chronicle.Grains.Observation.Reactors.Clients;
using Cratis.Chronicle.Grains.Observation.Reducers.Clients;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Services.Events.Constraints;
using Cratis.Chronicle.Services.EventSequences;
using Cratis.Chronicle.Services.Observation;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Execution;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Defines extensions for <see cref="ISiloBuilder"/> for configuring Chronicle in the current silo.
/// </summary>
public static class ChronicleServerSiloBuilderExtensions
{
    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="configure">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicleToSilo(this ISiloBuilder builder, Action<IChronicleBuilder>? configure = default)
    {
        builder.AddIncomingGrainCallFilter<CorrelationIdIncomingCallFilter>();
        builder.AddOutgoingGrainCallFilter<CorrelationIdOutgoingCallFilter>();
        builder.Services.TryAddSingleton<Cratis.Execution.CorrelationIdAccessor>();
        builder.Services.TryAddSingleton<ICorrelationIdAccessor, Cratis.Chronicle.Setup.Execution.CorrelationIdAccessor>();

        builder.Services.TryAddSingleton<IJobTypes, JobTypes>();
        builder
            .AddChronicleServicesAsInMemory()
            .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.NamespaceAdded, _ => _.FireAndForgetDelivery = true)
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ConstraintsChanged, _ => _.FireAndForgetDelivery = true)
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ReloadState, _ => _.FireAndForgetDelivery = true)
            .AddReplayStateManagement()
            .AddProjectionsService()
            .AddReminders()
            .AddMemoryGrainStorage("PubSubStore") // TODO: Store Grain state in Database
            .AddStreaming()
            .AddMemoryStreams(WellKnownStreamProviders.ProjectionChangesets)
            .AddStorageProviders()
            .ConfigureSerialization();

        builder.Services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>, ChronicleServerStartupTask>();

        builder.Services.AddChronicleMeters();
        var chronicleBuilder = new ChronicleBuilder(builder, builder.Services, builder.Configuration);
        configure?.Invoke(chronicleBuilder);
        return builder;
    }

    /// <summary>
    /// Add Chronicle services to the silo as in-memory versions rather than using gRPC when used internally.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicleServicesAsInMemory(this ISiloBuilder builder)
    {
        builder.Services.AddSingleton<IServices>(sp =>
        {
            var grainFactory = sp.GetRequiredService<IGrainFactory>();
            var clusterClient = sp.GetRequiredService<IClusterClient>();
            var storage = sp.GetRequiredService<IStorage>();
            var expandoObjectConverter = sp.GetRequiredService<IExpandoObjectConverter>();
            var jsonSerializerOptions = sp.GetRequiredService<JsonSerializerOptions>();
            return new Cratis.Chronicle.Connections.Services(
                new Cratis.Chronicle.Services.EventStores(storage),
                new Cratis.Chronicle.Services.Namespaces(grainFactory, storage),
                new Cratis.Chronicle.Services.Recommendations.Recommendations(grainFactory, storage),
                new Cratis.Chronicle.Services.Identities.Identities(storage),
                new EventSequences(grainFactory, storage, Globals.JsonSerializerOptions),
                new EventTypes(storage),
                new Constraints(grainFactory),
                new Observers(grainFactory, storage),
                new FailedPartitions(storage),
                new Cratis.Chronicle.Services.Observation.Reactors.Reactors(grainFactory, sp.GetRequiredService<IReactorMediator>(), sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Observation.Reactors.Reactors>>()),
                new Cratis.Chronicle.Services.Observation.Reducers.Reducers(grainFactory, sp.GetRequiredService<IReducerMediator>(), expandoObjectConverter, sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Observation.Reducers.Reducers>>()),
                new Cratis.Chronicle.Services.Projections.Projections(clusterClient, grainFactory, jsonSerializerOptions),
                new Cratis.Chronicle.Services.Jobs.Jobs(grainFactory, storage),
                new Cratis.Chronicle.Services.Host.Server(clusterClient));
        });

        return builder;
    }
}
