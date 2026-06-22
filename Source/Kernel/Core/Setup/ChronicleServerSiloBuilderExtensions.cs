// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.EventSequences.Placement;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Placement;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Events.Constraints;
using Cratis.Chronicle.Services.EventSequences;
using Cratis.Chronicle.Services.Observation;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Execution;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Cratis.Types;
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
        builder.AddActivityPropagation();
        builder.AddIncomingGrainCallFilter<CorrelationIdIncomingCallFilter>();
        builder.AddOutgoingGrainCallFilter<CorrelationIdOutgoingCallFilter>();
        builder.AddIncomingGrainCallFilter<UserIdentityIncomingCallFilter>();
        builder.AddOutgoingGrainCallFilter<UserIdentityOutgoingCallFilter>();
        builder.Services.TryAddSingleton<Cratis.Execution.CorrelationIdAccessor>();
        builder.Services.TryAddSingleton<ICorrelationIdAccessor, Cratis.Chronicle.Setup.Execution.CorrelationIdAccessor>();

        builder.Services.TryAddSingleton<IEventTypes, EventTypes>();
        builder.Services.TryAddSingleton<IJobTypes, JobTypes>();
        builder.Services.TryAddSingleton<IJobStepThrottle, JobStepThrottle>();
        builder.Services.TryAddSingleton<ITypeFormats, TypeFormats>();
        builder.Services.TryAddSingleton<IExpandoObjectConverter, ExpandoObjectConverter>();
        builder.Services.TryAddSingleton<IEventCompliance, EventCompliance>();
        builder.Services.TryAddSingleton<IReadModelsCompliance, ReadModelsCompliance>();
        builder.Services.TryAddSingleton<IEventTypeMigrations, EventTypeMigrations>();
        builder
            .AddChronicleServicesAsInMemory()
            .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
            .AddPlacementDirector<EventSequencePlacementStrategy, EventSequencePlacementDirector>()
            .AddPlacementDirector<ObserverPlacementStrategy, ObserverPlacementDirector>()
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.NamespaceAdded, _ => _.FireAndForgetDelivery = true)
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ConstraintsChanged, _ => _.FireAndForgetDelivery = true)
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ReloadState, _ => _.FireAndForgetDelivery = true)
            .AddReplayStateManagement()
            .AddProjectionsService()
            .AddReminders()
            .AddMemoryGrainStorage("PubSubStore") // TODO: Store Grain state in Database
            .AddStorageProviders()
            .AddWebhookObserverHttpClient()
            .ConfigureSerialization();

        builder.Services.AddSingleton(sp => sp.GetRequiredService<IStorage>().System.Users);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IStorage>().System.Applications);
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
            var projections = new Cratis.Chronicle.Services.Projections.Projections(grainFactory, expandoObjectConverter, sp.GetRequiredService<ILanguageService>(), sp);
            return new Cratis.Chronicle.Contracts.Services(
                new Cratis.Chronicle.Services.Compliance.ComplianceService(
                    sp.GetRequiredService<IJsonComplianceManager>(),
                    sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Compliance.ComplianceService>>()),
                new Cratis.Chronicle.Services.EventStores(
                    grainFactory,
                    storage,
                    sp.GetRequiredService<IEventTypes>(),
                    sp.GetRequiredService<Cratis.Chronicle.Observation.Reactors.Kernel.IReactors>()),
                new Cratis.Chronicle.Services.Namespaces(grainFactory, storage),
                new Cratis.Chronicle.Services.Recommendations.Recommendations(grainFactory, storage),
                new Cratis.Chronicle.Services.Identities.Identities(storage),
                new EventSequences(
                    grainFactory,
                    storage,
                    sp.GetRequiredService<IEventCompliance>(),
                    jsonSerializerOptions),
                new Cratis.Chronicle.Services.Events.EventTypes(storage, grainFactory),
                new Constraints(grainFactory),
                new Cratis.Chronicle.Services.Observation.Observers(grainFactory, storage),
                new FailedPartitions(storage),
                new Cratis.Chronicle.Services.Observation.Reactors.Reactors(
                    grainFactory,
                    sp.GetRequiredService<IReactorMediator>(),
                    sp.GetRequiredService<IStorage>(),
                    jsonSerializerOptions,
                    sp.GetRequiredKeyedService<Cratis.Traces.IActivitySource<Cratis.Chronicle.Services.Observation.Reactors.Reactors>>(Cratis.Chronicle.Concepts.WellKnown.MeterName),
                    sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Observation.Reactors.Reactors>>()),
                new Cratis.Chronicle.Services.Observation.Reducers.Reducers(grainFactory, sp.GetRequiredService<IReducerMediator>(), expandoObjectConverter, jsonSerializerOptions, sp.GetRequiredKeyedService<Cratis.Traces.IActivitySource<Cratis.Chronicle.Services.Observation.Reducers.Reducers>>(Cratis.Chronicle.Concepts.WellKnown.MeterName), sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Observation.Reducers.Reducers>>()),
                projections,
                new Cratis.Chronicle.Services.Observation.Webhooks.Webhooks(grainFactory, storage, sp.GetRequiredService<IWebhookDefinitionComparer>(), sp.GetRequiredService<Cratis.Chronicle.Security.IEncryption>(), sp.GetRequiredService<IOAuthClient>(), sp.GetRequiredService<IWebhookMediator>()),
                new Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions(grainFactory, storage),
                new Cratis.Chronicle.Services.ReadModels.ReadModels(grainFactory, storage, expandoObjectConverter, sp.GetRequiredService<IReducerMediator>(), sp.GetRequiredService<IReadModelsCompliance>(), sp.GetRequiredService<IEventCompliance>(), jsonSerializerOptions),
                new Cratis.Chronicle.Services.ReadModels.MaterializedReadModels(grainFactory, storage, sp.GetRequiredService<IReadModelsCompliance>()),
                new Cratis.Chronicle.Services.Jobs.Jobs(grainFactory, storage),
                new Cratis.Chronicle.Services.Seeding.EventSeeding(grainFactory),
                new Cratis.Chronicle.Services.Security.Users(grainFactory, storage),
                new Cratis.Chronicle.Services.Security.Applications(grainFactory, storage),
                new Cratis.Chronicle.Services.Host.Server(
                    clusterClient,
                    grainFactory,
                    sp.GetRequiredService<Cratis.Chronicle.Projections.Engine.Pipelines.IProjectionPipelineManager>(),
                    sp.GetRequiredService<IInstancesOf<ICanPerformKernelStateReset>>(),
                    sp.GetRequiredService<KernelBootstrapResetHandler>()));
        });

        return builder;
    }
}
