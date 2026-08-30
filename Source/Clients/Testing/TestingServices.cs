// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelGrpc;

using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Captures;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Patterns;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KernelApplicationsService = KernelGrpc::Cratis.Chronicle.Services.Security.Applications;
using KernelCaptureLanguageService = KernelCore::Cratis.Chronicle.Captures.Engine.DeclarationLanguage.LanguageService;
using KernelCapturesService = KernelGrpc::Cratis.Chronicle.Services.Captures.Captures;
using KernelCaptureValidator = KernelCore::Cratis.Chronicle.Captures.Engine.CaptureValidator;
using KernelComplianceService = KernelGrpc::Cratis.Chronicle.Services.Compliance.ComplianceService;
using KernelConstraintsService = KernelGrpc::Cratis.Chronicle.Services.Events.Constraints.Constraints;
using KernelEventCompliance = KernelCore::Cratis.Chronicle.Events.EventCompliance;
using KernelEventStoresService = KernelGrpc::Cratis.Chronicle.Services.EventStores.EventStores;
using KernelEventTypeRegistrar = KernelCore::Cratis.Chronicle.EventTypes.EventTypeRegistrar;
using KernelEventTypesService = KernelGrpc::Cratis.Chronicle.Services.EventTypes.EventTypes;
using KernelExternalServicesService = KernelGrpc::Cratis.Chronicle.Services.ExternalServices.ExternalServices;
using KernelFacetSetGenerator = KernelCore::Cratis.Chronicle.Patterns.FacetSetGenerator;
using KernelFacetVocabulary = KernelCore::Cratis.Chronicle.Patterns.FacetVocabulary;
using KernelFailedPartitionsService = KernelGrpc::Cratis.Chronicle.Services.Observation.FailedPartitions;
using KernelIdentitiesService = KernelGrpc::Cratis.Chronicle.Services.Identities.Identities;
using KernelJobsService = KernelGrpc::Cratis.Chronicle.Services.Jobs.Jobs;
using KernelJsonComplianceManager = KernelCore::Cratis.Chronicle.Compliance.JsonComplianceManager;
using KernelJsonCompliancePropertyValueHandler = KernelCore::Cratis.Chronicle.Compliance.IJsonCompliancePropertyValueHandler;
using KernelMaterializedReadModelStore = KernelCore::Cratis.Chronicle.ReadModels.MaterializedReadModelStore;
using KernelNamespacesService = KernelGrpc::Cratis.Chronicle.Services.Namespaces.Namespaces;
using KernelObserversService = KernelGrpc::Cratis.Chronicle.Services.Observation.Observers;
using KernelPatternMatcher = KernelCore::Cratis.Chronicle.Patterns.PatternMatcher;
using KernelPatternsService = KernelGrpc::Cratis.Chronicle.Services.Patterns.Patterns;
using KernelProjectionChangesetMediator = KernelCore::Cratis.Chronicle.Projections.ProjectionChangesetMediator;
using KernelProjectionsService = KernelGrpc::Cratis.Chronicle.Services.Projections.Projections;
using KernelReactorMediator = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.ReactorMediator;
using KernelReactorsService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reactors.Reactors;
using KernelReadModelsCompliance = KernelCore::Cratis.Chronicle.ReadModels.ReadModelsCompliance;
using KernelReadModelsService = KernelGrpc::Cratis.Chronicle.Services.ReadModels.ReadModels;
using KernelRecommendationsService = KernelGrpc::Cratis.Chronicle.Services.Recommendations.Recommendations;
using KernelReducerMediator = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.ReducerMediator;
using KernelReducersService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reducers.Reducers;
using KernelSeedingService = KernelGrpc::Cratis.Chronicle.Services.Seeding.EventSeeding;
using KernelSequencesService = KernelGrpc::Cratis.Chronicle.Services.Sequences.EventSequences;
using KernelServerService = KernelGrpc::Cratis.Chronicle.Services.Host.Server;
using KernelSubscriptionsService = KernelGrpc::Cratis.Chronicle.Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions;
using KernelUsersService = KernelGrpc::Cratis.Chronicle.Services.Security.Users;
using KernelWebhookComparer = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookDefinitionComparer;
using KernelWebhookMediatorImpl = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookMediator;
using KernelWebhookRegistrar = KernelCore::Cratis.Chronicle.Observation.Webhooks.WebhookRegistrar;
using KernelWebhooksService = KernelGrpc::Cratis.Chronicle.Services.Observation.Webhooks.Webhooks;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an implementation of <see cref="IServices"/> for testing scenarios backed by real kernel
/// gRPC service implementations wired to in-memory storage.
/// </summary>
/// <remarks>
/// All gRPC service contracts are backed by the real kernel implementations from
/// <c>Cratis.Chronicle.Services</c>. Command-dispatching implementations execute through one shared Arc
/// command pipeline, whose service provider carries the in-memory collaborators the command handlers
/// resolve their parameters from.
/// </remarks>
internal sealed class TestingServices : IServices
{
    readonly Lazy<IObservers> _observers;
    readonly Lazy<IFailedPartitions> _failedPartitions;
    readonly Lazy<IReactors> _reactors;
    readonly Lazy<IReducers> _reducers;
    readonly Lazy<IProjections> _projections;
    readonly Lazy<IWebhooks> _webhooks;
    readonly Lazy<IExternalServices> _externalServices;
    readonly Lazy<ICaptures> _captures;
    readonly Lazy<IEventStoreSubscriptions> _eventStoreSubscriptions;
    readonly Lazy<IJobs> _jobs;
    readonly Lazy<IEventSeeding> _seeding;
    readonly Lazy<Contracts.Sequences.IEventSequences> _sequences;
    readonly Lazy<INamespaces> _namespaces;
    readonly Lazy<IIdentities> _identities;
    readonly Lazy<IEventTypes> _eventTypes;
    readonly Lazy<IPatterns> _patterns;
    readonly Lazy<IRecommendations> _recommendations;
    readonly Lazy<IConstraints> _constraints;
    readonly Lazy<IUsers> _users;
    readonly Lazy<IApplications> _applications;
    readonly Lazy<IServer> _server;
    readonly Lazy<IEventStores> _eventStores;
    readonly Lazy<IReadModels> _readModels;
    readonly Lazy<ICompliance> _compliance;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestingServices"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for grain-based operations.</param>
    /// <param name="storage">The <see cref="IStorage"/> backed by in-memory implementations.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    public TestingServices(
        IGrainFactory grainFactory,
        IStorage storage,
        JsonSerializerOptions jsonSerializerOptions)
    {
        // One pipeline serves every command-dispatching service. Its provider carries the collaborators the
        // command handlers resolve their parameters from - the same instances the service constructors used
        // to receive directly before dispatch moved to the Arc command pipeline.
        var commandPipeline = new Lazy<Cratis.Arc.Commands.ICommandPipeline>(() =>
            InProcessCommandPipeline.Create(
                grainFactory,
                storage,
                jsonSerializerOptions,
                services =>
                {
                    services.AddSingleton<KernelCore::Cratis.Chronicle.Events.IEventCompliance>(new KernelEventCompliance(
                        new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                        new ExpandoObjectConverter(new TypeFormats())));
                    services.AddSingleton<KernelCore::Cratis.Chronicle.EventTypes.IEventTypesCacheClient>(new EventSequences.NoOpEventTypesCacheClient());
                    services.AddSingleton(new KernelEventTypeRegistrar(grainFactory));
                    services.AddSingleton<KernelCore::Cratis.Chronicle.Captures.Engine.DeclarationLanguage.ILanguageService>(new KernelCaptureLanguageService());
                    services.AddSingleton<KernelCore::Cratis.Chronicle.Captures.Engine.ICaptureValidator>(new KernelCaptureValidator(storage));
                    services.AddSingleton(new KernelWebhookRegistrar(
                        grainFactory,
                        new KernelWebhookComparer(
                            storage,
                            new ObjectComparer(),
                            NullLogger<KernelWebhookComparer>.Instance),
                        null!,
                        null!,
                        new KernelWebhookMediatorImpl(null!, jsonSerializerOptions),
                        Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions())));
                }));

        _observers = new(() => new KernelObserversService(grainFactory, storage));

        _failedPartitions = new(() => new KernelFailedPartitionsService(storage));

        _reactors = new(() =>
            new KernelReactorsService(
                grainFactory,
                new KernelReactorMediator(),
                storage,
                jsonSerializerOptions,
                new ActivitySource<KernelReactorsService>(),
                NullLogger<KernelReactorsService>.Instance));

        _reducers = new(() =>
            new KernelReducersService(
                grainFactory,
                new KernelReducerMediator(),
                new ExpandoObjectConverter(new TypeFormats()),
                jsonSerializerOptions,
                new ActivitySource<KernelReducersService>(),
                NullLogger<KernelReducersService>.Instance));

        _projections = new(() =>
            new KernelProjectionsService(
                grainFactory,
                new ExpandoObjectConverter(new TypeFormats()),
                null!,
                null!));

        _webhooks = new(() =>
            new KernelWebhooksService(
                commandPipeline.Value,
                storage,
                NullLogger<KernelWebhooksService>.Instance));

        _externalServices = new(() =>
            new KernelExternalServicesService(commandPipeline.Value, storage, NullLogger<KernelExternalServicesService>.Instance));

        _captures = new(() =>
            new KernelCapturesService(
                commandPipeline.Value,
                storage,
                jsonSerializerOptions,
                NullLogger<KernelCapturesService>.Instance));

        _eventStoreSubscriptions = new(() =>
            new KernelSubscriptionsService(grainFactory, storage, Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions())));

        _jobs = new(() =>
            new KernelJobsService(commandPipeline.Value, storage, grainFactory, NullLogger<KernelJobsService>.Instance));

        _seeding = new(() =>
            new KernelSeedingService(commandPipeline.Value, grainFactory, NullLogger<KernelSeedingService>.Instance));

        _sequences = new(() =>
            new KernelSequencesService(
                commandPipeline.Value,
                storage,
                new KernelEventCompliance(
                    new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                    new ExpandoObjectConverter(new TypeFormats())),
                jsonSerializerOptions,
                new EventSequences.InProcessQueryContextManager(),
                grainFactory,
                NullLogger<KernelSequencesService>.Instance));

        _namespaces = new(() =>
            new KernelNamespacesService(commandPipeline.Value, storage, NullLogger<KernelNamespacesService>.Instance));

        _identities = new(() =>
            new KernelIdentitiesService(commandPipeline.Value, storage, NullLogger<KernelIdentitiesService>.Instance));

        _eventTypes = new(() =>
            new KernelEventTypesService(
                commandPipeline.Value,
                storage,
                NullLogger<KernelEventTypesService>.Instance));

        _patterns = new(() =>
        {
            var patternOptions = Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions());
            return new KernelPatternsService(
                storage,
                new KernelFacetVocabulary(patternOptions),
                new KernelFacetSetGenerator(),
                new KernelPatternMatcher(),
                patternOptions,
                NullLogger<KernelPatternsService>.Instance);
        });

        _recommendations = new(() =>
            new KernelRecommendationsService(commandPipeline.Value, storage, NullLogger<KernelRecommendationsService>.Instance));

        _constraints = new(() => new KernelConstraintsService(grainFactory));

        _users = new(() =>
            new KernelUsersService(commandPipeline.Value, storage, NullLogger<KernelUsersService>.Instance));

        _applications = new(() =>
            new KernelApplicationsService(commandPipeline.Value, storage, NullLogger<KernelApplicationsService>.Instance));

        _server = new(() => new KernelServerService(null!));

        _eventStores = new(() =>
            new KernelEventStoresService(commandPipeline.Value, storage, NullLogger<KernelEventStoresService>.Instance));

        _readModels = new(() =>
            new KernelReadModelsService(
                grainFactory,
                storage,
                new ExpandoObjectConverter(new TypeFormats()),
                new KernelReducerMediator(),
                new KernelProjectionChangesetMediator(),

                // Live read-model watching is not supported by the in-process scenario harness (grain and
                // object-reference lookups throw NotSupportedException), so no local silo details are needed.
                null!,
                new KernelReadModelsCompliance(
                    new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                    new ExpandoObjectConverter(new TypeFormats())),
                new KernelEventCompliance(
                    new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                    new ExpandoObjectConverter(new TypeFormats())),
                new KernelMaterializedReadModelStore(
                    storage,
                    new KernelReadModelsCompliance(
                        new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                        new ExpandoObjectConverter(new TypeFormats()))),
                jsonSerializerOptions));

        _compliance = new(() =>
            new KernelComplianceService(
                grainFactory,
                new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                NullLogger<KernelComplianceService>.Instance));
    }

    /// <inheritdoc/>
    public IReadModels ReadModels => _readModels.Value;

    /// <inheritdoc/>
    public IMaterializedReadModels MaterializedReadModels => throw new NotSupportedException("MaterializedReadModels is not supported in test scenarios.");

    /// <inheritdoc/>
    public ICompliance Compliance => _compliance.Value;

    /// <inheritdoc/>
    public IConstraints Constraints => _constraints.Value;

    /// <inheritdoc/>
    public IObservers Observers => _observers.Value;

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions => _failedPartitions.Value;

    /// <inheritdoc/>
    public IReactors Reactors => _reactors.Value;

    /// <inheritdoc/>
    public IReducers Reducers => _reducers.Value;

    /// <inheritdoc/>
    public IProjections Projections => _projections.Value;

    /// <inheritdoc/>
    public IWebhooks Webhooks => _webhooks.Value;

    /// <inheritdoc/>
    public IExternalServices ExternalServices => _externalServices.Value;

    /// <inheritdoc/>
    public ICaptures Captures => _captures.Value;

    /// <inheritdoc/>
    public IEventStoreSubscriptions EventStoreSubscriptions => _eventStoreSubscriptions.Value;

    /// <inheritdoc/>
    public IJobs Jobs => _jobs.Value;

    /// <inheritdoc/>
    public IEventSeeding Seeding => _seeding.Value;

    /// <inheritdoc/>
    public Contracts.Sequences.IEventSequences Sequences => _sequences.Value;

    /// <inheritdoc/>
    public IEventStores EventStores => _eventStores.Value;

    /// <inheritdoc/>
    public INamespaces Namespaces => _namespaces.Value;

    /// <inheritdoc/>
    public IIdentities Identities => _identities.Value;

    /// <inheritdoc/>
    public IEventTypes EventTypes => _eventTypes.Value;

    /// <inheritdoc/>
    public IPatterns Patterns => _patterns.Value;

    /// <inheritdoc/>
    public IRecommendations Recommendations => _recommendations.Value;

    /// <inheritdoc/>
    public IUsers Users => _users.Value;

    /// <inheritdoc/>
    public IApplications Applications => _applications.Value;

    /// <inheritdoc/>
    public IServer Server => _server.Value;

    /// <inheritdoc/>
    public Contracts.Clients.IConnectionService Connections => throw new NotSupportedException("Connections is not supported in test scenarios.");
}
