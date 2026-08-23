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
using KernelFailedPartitionsService = KernelGrpc::Cratis.Chronicle.Services.Observation.FailedPartitions;
using KernelIdentitiesService = KernelGrpc::Cratis.Chronicle.Services.Identities.Identities;
using KernelJobsService = KernelGrpc::Cratis.Chronicle.Services.Jobs.Jobs;
using KernelJsonComplianceManager = KernelCore::Cratis.Chronicle.Compliance.JsonComplianceManager;
using KernelJsonCompliancePropertyValueHandler = KernelCore::Cratis.Chronicle.Compliance.IJsonCompliancePropertyValueHandler;
using KernelMaterializedReadModelStore = KernelCore::Cratis.Chronicle.ReadModels.MaterializedReadModelStore;
using KernelNamespacesService = KernelGrpc::Cratis.Chronicle.Services.Namespaces.Namespaces;
using KernelObserversService = KernelGrpc::Cratis.Chronicle.Services.Observation.Observers;
using KernelProjectionChangesetMediator = KernelCore::Cratis.Chronicle.Projections.ProjectionChangesetMediator;
using KernelProjectionsService = KernelGrpc::Cratis.Chronicle.Services.Projections.Projections;
using KernelReactorMediator = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.ReactorMediator;
using KernelReactorsService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reactors.Reactors;
using KernelReadModelsCompliance = KernelCore::Cratis.Chronicle.ReadModels.ReadModelsCompliance;
using KernelReadModelsService = KernelGrpc::Cratis.Chronicle.Services.ReadModels.ReadModels;
using KernelRecommendationsService = KernelGrpc::Cratis.Chronicle.Services.Recommendations.Recommendations;
using KernelReducerMediator = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.ReducerMediator;
using KernelReducersService = KernelGrpc::Cratis.Chronicle.Services.Observation.Reducers.Reducers;
using KernelRequestCausation = KernelCore::Cratis.Chronicle.Sequences.RequestCausation;
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
/// <c>Cratis.Chronicle.Services</c>.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for grain-based operations.</param>
/// <param name="storage">The <see cref="IStorage"/> backed by in-memory implementations.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class TestingServices(
    IGrainFactory grainFactory,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions) : IServices
{
    readonly Lazy<IObservers> _observers = new(() =>
        new KernelObserversService(grainFactory, storage));

    readonly Lazy<IFailedPartitions> _failedPartitions = new(() =>
        new KernelFailedPartitionsService(storage));

    readonly Lazy<IReactors> _reactors = new(() =>
        new KernelReactorsService(
            grainFactory,
            new KernelReactorMediator(),
            storage,
            jsonSerializerOptions,
            new ActivitySource<KernelReactorsService>(),
            NullLogger<KernelReactorsService>.Instance));

    readonly Lazy<IReducers> _reducers = new(() =>
        new KernelReducersService(
            grainFactory,
            new KernelReducerMediator(),
            new ExpandoObjectConverter(new TypeFormats()),
            jsonSerializerOptions,
            new ActivitySource<KernelReducersService>(),
            NullLogger<KernelReducersService>.Instance));

    readonly Lazy<IProjections> _projections = new(() =>
        new KernelProjectionsService(
            grainFactory,
            new ExpandoObjectConverter(new TypeFormats()),
            null!,
            null!));

    readonly Lazy<IWebhooks> _webhooks = new(() =>
        new KernelWebhooksService(
            new KernelWebhookRegistrar(
                grainFactory,
                new KernelWebhookComparer(
                    storage,
                    new ObjectComparer(),
                    NullLogger<KernelWebhookComparer>.Instance),
                null!,
                null!,
                new KernelWebhookMediatorImpl(null!, jsonSerializerOptions),
                Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions())),
            storage,
            NullLogger<KernelWebhooksService>.Instance));

    readonly Lazy<IExternalServices> _externalServices = new(() =>
        new KernelExternalServicesService(storage, NullLogger<KernelExternalServicesService>.Instance));

    readonly Lazy<ICaptures> _captures = new(() =>
        new KernelCapturesService(
            grainFactory,
            storage,
            new KernelCaptureLanguageService(),
            new KernelCaptureValidator(storage),
            jsonSerializerOptions,
            NullLogger<KernelCapturesService>.Instance));

    readonly Lazy<IEventStoreSubscriptions> _eventStoreSubscriptions = new(() =>
        new KernelSubscriptionsService(grainFactory, storage, Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions())));

    readonly Lazy<IJobs> _jobs = new(() =>
        new KernelJobsService(grainFactory, storage, NullLogger<KernelJobsService>.Instance));

    readonly Lazy<IEventSeeding> _seeding = new(() =>
        new KernelSeedingService(grainFactory, NullLogger<KernelSeedingService>.Instance));

    readonly Lazy<Contracts.Sequences.IEventSequences> _sequences = new(() =>
        new KernelSequencesService(
            grainFactory,
            new KernelRequestCausation(new Microsoft.AspNetCore.Http.HttpContextAccessor()),
            new EventSequences.InProcessCurrentPrincipalAccessor(),
            storage,
            new KernelEventCompliance(
                new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
                new ExpandoObjectConverter(new TypeFormats())),
            jsonSerializerOptions,
            new EventSequences.InProcessQueryContextManager(),
            NullLogger<KernelSequencesService>.Instance));

    readonly Lazy<INamespaces> _namespaces = new(() =>
        new KernelNamespacesService(grainFactory, storage, NullLogger<KernelNamespacesService>.Instance));

    readonly Lazy<IIdentities> _identities = new(() =>
        new KernelIdentitiesService(storage, NullLogger<KernelIdentitiesService>.Instance));

    readonly Lazy<IEventTypes> _eventTypes = new(() =>
        new KernelEventTypesService(
            storage,
            new EventSequences.NoOpEventTypesCacheClient(),
            new KernelEventTypeRegistrar(grainFactory),
            NullLogger<KernelEventTypesService>.Instance));

    readonly Lazy<IRecommendations> _recommendations = new(() =>
        new KernelRecommendationsService(grainFactory, storage, NullLogger<KernelRecommendationsService>.Instance));

    readonly Lazy<IConstraints> _constraints = new(() =>
        new KernelConstraintsService(grainFactory));

    readonly Lazy<IUsers> _users = new(() =>
        new KernelUsersService(grainFactory, storage, NullLogger<KernelUsersService>.Instance));

    readonly Lazy<IApplications> _applications = new(() =>
        new KernelApplicationsService(grainFactory, storage, NullLogger<KernelApplicationsService>.Instance));

    readonly Lazy<IServer> _server = new(() =>
        new KernelServerService(null!));

    readonly Lazy<IEventStores> _eventStores = new(() =>
        new KernelEventStoresService(grainFactory, storage, null!, null!, NullLogger<KernelEventStoresService>.Instance));

    readonly Lazy<IReadModels> _readModels = new(() =>
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

    readonly Lazy<ICompliance> _compliance = new(() =>
        new KernelComplianceService(
            grainFactory,
            new KernelJsonComplianceManager(new KnownInstancesOf<KernelJsonCompliancePropertyValueHandler>(), NullLogger<KernelJsonComplianceManager>.Instance),
            NullLogger<KernelComplianceService>.Instance));

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
