// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc;
using Cratis.Chronicle;
using Cratis.Chronicle.Clients;
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
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Placement;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Services.Events.Constraints;
using Cratis.Chronicle.Services.Observation;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Execution;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        builder.Services.TryAddSingleton<IMaterializedReadModelStore, MaterializedReadModelStore>();
        builder.Services.TryAddSingleton<IEventTypeMigrations, EventTypeMigrations>();
        builder.Services.TryAddSingleton<IObserverSubscriberSelector, ObserverSubscriberSelector>();
        builder
            .AddChronicleServicesAsInMemory()
            .AddPlacementDirector<ConnectedClientsPlacementStrategy, ConnectedClientsPlacementDirector>()
            .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
            .AddPlacementDirector<EventSequencePlacementStrategy, EventSequencePlacementDirector>()
            .AddPlacementDirector<ObserverPlacementStrategy, ObserverPlacementDirector>()
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.NamespaceAdded, _ => _.FireAndForgetDelivery = true)
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ConstraintsChanged, _ => _.FireAndForgetDelivery = true)
            .AddReplayStateManagement()
            .AddEventTypesCacheInvalidation()
            .AddEncryptionKeyCacheInvalidation()
            .AddProjectionsService()
            .AddReminders()
            .AddMemoryGrainStorage("PubSubStore") // TODO: Store Grain state in Database
            .AddStorageProviders()
            .AddWebhookObserverHttpClient()
            .AddExternalServiceHttpClient()
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
        // The generated service implementations dispatch commands through the Arc ICommandPipeline - the
        // same pipeline the HTTP surface runs. A host that runs the full Arc setup has it already; one that
        // does not gets the Arc core services here - AddCratisArcCore rather than just the command services,
        // because the pipeline's validation filter needs the validator discovery it registers; a pipeline
        // without it would silently skip every validator. The Arc registrations do not guard against double
        // registration, so they are only added when the pipeline is absent.
        if (builder.Services.All(descriptor => descriptor.ServiceType != typeof(Cratis.Arc.Commands.ICommandPipeline)))
        {
            builder.Services.AddCratisArcCore();
        }

        builder.Services.AddSingleton<IServices>(sp =>
        {
            var grainFactory = sp.GetRequiredService<IGrainFactory>();
            var storage = sp.GetRequiredService<IStorage>();
            var expandoObjectConverter = sp.GetRequiredService<IExpandoObjectConverter>();
            var jsonSerializerOptions = sp.GetRequiredService<JsonSerializerOptions>();
            var projections = new Cratis.Chronicle.Services.Projections.Projections(grainFactory, expandoObjectConverter, sp.GetRequiredService<ILanguageService>(), sp);

            // The generated implementations declare their dependencies through their primary constructors,
            // which change whenever the generator's dispatch shape does. Constructing them through
            // ActivatorUtilities keeps this composition from repeating - and drifting from - those
            // constructor signatures.
            return new Cratis.Chronicle.Contracts.Services(
                new Cratis.Chronicle.Services.Compliance.ComplianceService(
                    grainFactory,
                    sp.GetRequiredService<IJsonComplianceManager>(),
                    sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Compliance.ComplianceService>>()),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.EventStores.EventStores>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Namespaces.Namespaces>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Recommendations.Recommendations>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Patterns.Patterns>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Identities.Identities>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Sequences.EventSequences>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.EventTypes.EventTypes>(sp),
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
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Observation.Webhooks.Webhooks>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.ExternalServices.ExternalServices>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Captures.Captures>(sp),
                new Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions(grainFactory, storage, sp.GetRequiredService<IOptions<ChronicleOptions>>()),
                new Cratis.Chronicle.Services.ReadModels.ReadModels(grainFactory, storage, expandoObjectConverter, sp.GetRequiredService<IReducerMediator>(), sp.GetRequiredService<Cratis.Chronicle.Projections.IProjectionChangesetMediator>(), sp.GetRequiredService<Orleans.Runtime.ILocalSiloDetails>(), sp.GetRequiredService<IReadModelsCompliance>(), sp.GetRequiredService<IMaterializedReadModelStore>(), jsonSerializerOptions),
                new Cratis.Chronicle.Services.ReadModels.MaterializedReadModels(grainFactory, storage, sp.GetRequiredService<IReadModelsCompliance>()),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.ReadModelExplorer.ReadModelExplorer>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Jobs.Jobs>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Seeding.EventSeeding>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Security.Users>(sp),
                ActivatorUtilities.CreateInstance<Cratis.Chronicle.Services.Security.Applications>(sp),
                new Cratis.Chronicle.Services.Host.Server(
                    sp.GetRequiredService<Cratis.Chronicle.DevelopmentTools.KernelStateResetter>()),
                new Cratis.Chronicle.Services.Clients.ConnectionService(
                    grainFactory,
                    sp.GetRequiredService<ILocalSiloDetails>(),
                    sp.GetRequiredService<Cratis.Chronicle.Clients.ConnectedClientsQuery>(),
                    sp.GetRequiredService<ILogger<Cratis.Chronicle.Services.Clients.ConnectionService>>(),
                    sp.GetRequiredService<IOptions<ChronicleOptions>>()));
        });

        return builder;
    }
}
